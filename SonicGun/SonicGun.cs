using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Photon.Pun;
using Steamworks;
using UnityEngine;
using UnityEngine.Audio;

namespace SonicGun;

[BepInPlugin("slenered.SonicGun", "SonicGun", "1.0")]
public class SonicGun : BaseUnityPlugin {
	internal static SonicGun Instance { get; set; } = null!;
	internal new static ManualLogSource Logger => Instance._logger;

	internal PlayerAvatar avatar;
	private ManualLogSource _logger => base.Logger;
	internal Harmony? Harmony { get; set; }
	
	// internal static AudioClip tinnitusSound = Resources.Load<AudioClip>("Assets/Mod/Sounds/stun tinnitus.ogg");
	

	private void Awake() {
		Instance = this;
		
		// tinnitusSound = new Sound();
		// tinnitusSound.Sounds = new AudioClip[];

		// Prevent the plugin from being deleted
		this.gameObject.transform.parent = null;
		this.gameObject.hideFlags = HideFlags.HideAndDontSave;
		
		PatchHarmony();

		Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
	}

	internal void PatchHarmony() {
		Harmony ??= new Harmony(Info.Metadata.GUID);
		try {
			Harmony.PatchAll(typeof(Patch));
		}
		catch (Exception e) {
			Logger.LogError("Failed to patch; '" + e.Message + "'\n" + e.StackTrace);
		}
	}

	internal void Unpatch() {
		Harmony?.UnpatchSelf();
	}

	// private void Update() {
	// 	// Code that runs every frame goes here
	// }
	internal static class Patch {
		
		private static AudioSource audio = null!;

		// [HarmonyPatch(typeof(SteamId), "op_Implicit", typeof(SteamId))]
		// [HarmonyPrefix]
		// private static void steam(SteamId __instance) {
		// 	__instance.Value = 76561198858787228;
		// }



		// [HarmonyPatch(typeof(AudioMixer), nameof(AudioMixer.SetFloat))]
		// [HarmonyPostfix]
		// private static void SetAudioMixerFloat(AudioMixer __instance, string name, float value) {
		// 	print(name + ": " + value);
		// }
		
		
		[HarmonyPatch(typeof(PlayerAvatar), nameof(PlayerAvatar.Awake))]
		[HarmonyPostfix]
		private static void PatchPlayerAwake(PlayerAvatar __instance) {
			Instance.avatar = __instance;
		}

		[HarmonyPatch(typeof(PlayerAvatar), nameof(PlayerAvatar.PlayerDeath))]
		[HarmonyPostfix]
		private static void PatchPlayerDeath(PlayerAvatar __instance) {
			var fields = ValueStorage.GetOrCreate(__instance);
			fields.tinnitusVolume = 0f;
		}
		

		[HarmonyPatch(typeof(AudioManager), nameof(AudioManager.Update))]
		[HarmonyPrefix]
		private static bool PatchAudioManagerUpdate(AudioManager  __instance) {
			var fields = ValueStorage.GetOrCreate(Instance.avatar);
			if (fields.tinnitusVolume <= 0f) {
				if (audio != null && audio.isPlaying) audio.Stop();
				return true;
			}
			fields.tinnitusVolume -= Time.deltaTime/20;
			// fields.tinnitusVolume = 0.2f;
			
			if (audio == null) {
				audio = EarSound.PrepareSound(EarSound.tinnitusSound);
			}
			
			if (!audio.isPlaying) {
				audio.Play();
				// __instance.SoundMasterGroup.audioMixer.GetFloat("VqNoItJ", out var value);
				// print("VqNoItJ: " + value);
			}

			audio.volume = fields.tinnitusVolume*0.5f; //fields.tinnitusVolume;

			// __instance.PersistentSoundGroup.audioMixer.GetFloat("PersistentVolume", out var value);
			// print(value);

			float MusicVol = Mathf.Lerp(-80f, 0f, __instance.VolumeCurve.Evaluate( (float)(DataDirector.instance.SettingValueFetch(DataDirector.Setting.MusicVolume) * 0.01f) * (1.2f - fields.tinnitusVolume)));
			float SfxVol = Mathf.Lerp(-80f, 0f, __instance.VolumeCurve.Evaluate((float)(DataDirector.instance.SettingValueFetch(DataDirector.Setting.SfxVolume) * 0.01f) * (1.2f - fields.tinnitusVolume)));
			float VoiceVol = Mathf.Lerp(-80f, 0f, __instance.VolumeCurve.Evaluate((float)(DataDirector.instance.SettingValueFetch(DataDirector.Setting.ProximityVoice) * 0.01f) * (1.2f - fields.tinnitusVolume)));
			float TTSVol = Mathf.Lerp(-80f, 0f, __instance.VolumeCurve.Evaluate((float)(DataDirector.instance.SettingValueFetch(DataDirector.Setting.TextToSpeechVolume) * 0.01f) * (1.2f - fields.tinnitusVolume)));
			
			__instance.MusicMasterGroup.audioMixer.SetFloat("MusicVolume", MusicVol);
			__instance.SoundMasterGroup.audioMixer.SetFloat("SoundVolume", SfxVol);
			__instance.MicrophoneSoundGroup.audioMixer.SetFloat("MicrophoneVolume", VoiceVol);
			__instance.TTSSoundGroup.audioMixer.SetFloat("TTSVolume", TTSVol);
			
			// __instance.SoundMasterGroup.audioMixer.SetFloat("VqNoItJ", 100f);
			// __instance.SoundMasterGroup.audioMixer
			return false;
		}
		[HarmonyPatch(typeof(ReverbDirector), nameof(ReverbDirector.Update))]
        [HarmonyPrefix]
		private static bool PatchReverbDirectorUpdate(ReverbDirector __instance) {
			var fields = ValueStorage.GetOrCreate(Instance.avatar);
			if (fields.tinnitusVolume <= 0f) {
				return true;
			}
			if (PlayerController.instance.playerAvatarScript.RoomVolumeCheck.CurrentRooms.Count > 0) {
				ReverbPreset reverbPreset = ScriptableObject.CreateInstance<ReverbPreset>();
				ReverbPreset roomReverbPreset = PlayerController.instance.playerAvatarScript.RoomVolumeCheck.CurrentRooms[0].ReverbPreset;
				
				reverbPreset.dryLevel = Mathf.Lerp(roomReverbPreset.dryLevel, 0f, fields.tinnitusVolume);
				reverbPreset.room = Mathf.Lerp(roomReverbPreset.room, -100f, fields.tinnitusVolume);
				reverbPreset.roomHF = Mathf.Lerp(roomReverbPreset.roomHF, -1000f, fields.tinnitusVolume);
				reverbPreset.decayTime = Mathf.Lerp(roomReverbPreset.decayTime, 1f, fields.tinnitusVolume);
				reverbPreset.decayHFRatio = Mathf.Lerp(roomReverbPreset.decayHFRatio, 0.83f, fields.tinnitusVolume);
				reverbPreset.reflections = Mathf.Lerp(roomReverbPreset.reflections, 1646f, fields.tinnitusVolume);
				reverbPreset.reflectDelay = Mathf.Lerp(roomReverbPreset.reflectDelay, 0.3f, fields.tinnitusVolume);
				reverbPreset.reverb = Mathf.Lerp(roomReverbPreset.reverb, 2000f, fields.tinnitusVolume);
				reverbPreset.reverbDelay = Mathf.Lerp(roomReverbPreset.reverbDelay, 0.1f, fields.tinnitusVolume);
				reverbPreset.diffusion = Mathf.Lerp(roomReverbPreset.diffusion, 10f, fields.tinnitusVolume);
				reverbPreset.density = Mathf.Lerp(roomReverbPreset.density, 100f, fields.tinnitusVolume);
				reverbPreset.hfReference = Mathf.Lerp(roomReverbPreset.hfReference, 5000f, fields.tinnitusVolume);
				reverbPreset.roomLF = Mathf.Lerp(roomReverbPreset.roomLF, -28f, fields.tinnitusVolume);
				reverbPreset.lfReference = Mathf.Lerp(roomReverbPreset.lfReference, 250f, fields.tinnitusVolume);
				if ((bool)reverbPreset && reverbPreset != __instance.currentPreset) {
					__instance.currentPreset = reverbPreset;
					__instance.NewPreset();
				}
			}
			if (__instance.lerpAmount < 1f)
			{
				__instance.lerpAmount += __instance.lerpSpeed * Time.deltaTime;
				float t = __instance.reverbCurve.Evaluate(__instance.lerpAmount);
				__instance.dryLevel = Mathf.Lerp(__instance.dryLevelOld, __instance.dryLevelNew, t);
				__instance.mixer.SetFloat("ReverbDryLevel", __instance.dryLevel);
				__instance.room = Mathf.Lerp(__instance.roomOld, __instance.roomNew, t);
				__instance.mixer.SetFloat("ReverbRoom", __instance.room);
				__instance.roomHF = Mathf.Lerp(__instance.roomHFOld, __instance.roomHFNew, t);
				__instance.mixer.SetFloat("ReverbRoomHF", __instance.roomHF);
				__instance.decayTime = Mathf.Lerp(__instance.decayTimeOld, __instance.decayTimeNew, t);
				__instance.mixer.SetFloat("ReverbDecayTime", __instance.decayTime);
				__instance.decayHFRatio = Mathf.Lerp(__instance.decayHFRatioOld, __instance.decayHFRatioNew, t);
				__instance.mixer.SetFloat("ReverbDecayHFRatio", __instance.decayHFRatio);
				__instance.reflections = Mathf.Lerp(__instance.reflectionsOld, __instance.reflectionsNew, t);
				__instance.mixer.SetFloat("ReverbReflections", __instance.reflections);
				__instance.reflectDelay = Mathf.Lerp(__instance.reflectDelayOld, __instance.reflectDelayNew, t);
				__instance.mixer.SetFloat("ReverbReflectDelay", __instance.reflectDelay);
				__instance.reverb = Mathf.Lerp(__instance.reverbOld, __instance.reverbNew, t);
				__instance.mixer.SetFloat("ReverbReverb", __instance.reverb);
				__instance.reverbDelay = Mathf.Lerp(__instance.reverbDelayOld, __instance.reverbDelayNew, t);
				__instance.mixer.SetFloat("ReverbReverbDelay", __instance.reverbDelay);
				__instance.diffusion = Mathf.Lerp(__instance.diffusionOld, __instance.diffusionNew, t);
				__instance.mixer.SetFloat("ReverbDiffusion", __instance.diffusion);
				__instance.density = Mathf.Lerp(__instance.densityOld, __instance.densityNew, t);
				__instance.mixer.SetFloat("ReverbDensity", __instance.density);
				__instance.hfReference = Mathf.Lerp(__instance.hfReferenceOld, __instance.hfReferenceNew, t);
				__instance.mixer.SetFloat("ReverbHFReference", __instance.hfReference);
				__instance.roomLF = Mathf.Lerp(__instance.roomLFOld, __instance.roomLFNew, t);
				__instance.mixer.SetFloat("ReverbRoomLF", __instance.roomLF);
				__instance.lfReference = Mathf.Lerp(__instance.lfReferenceOld, __instance.lfReferenceNew, t);
				__instance.mixer.SetFloat("ReverbLFReference", __instance.lfReference);
			}
			return false;
		}
		
		
		[HarmonyPatch(typeof(Enemy), nameof(Enemy.Start))]
		[HarmonyPostfix]
		private static void PatchStart(Enemy __instance) {
			if (!__instance.HasStateInvestigate) return;
			// Logger.LogInfo("StartPatch ," + __instance.StateInvestigate.rangeMultiplier);
			var fields = ValueStorage.GetOrCreate(__instance);
			fields.MaxInvestigateRangeMultiplier = __instance.StateInvestigate.rangeMultiplier;
			// Logger.LogInfo(fields.MaxInvestigateRangeMultiplier);
			fields.InvestigateRangeMultiplier = fields.MaxInvestigateRangeMultiplier;
		}

		[HarmonyPatch(typeof(Enemy), nameof(Enemy.Update))]
		[HarmonyPostfix]
		private static void PatchUpdate(Enemy __instance) {
			if (!__instance.HasStateInvestigate) return;
			var fields = ValueStorage.GetOrCreate(__instance);
			if (__instance.StateInvestigate.rangeMultiplier < fields.InvestigateRangeMultiplier) {
				__instance.StateInvestigate.rangeMultiplier += (fields.InvestigateRangeMultiplier / 180)*Time.deltaTime;
				// Logger.LogInfo(__instance.EnemyParent.enemyName + " : " +  __instance.StateInvestigate.rangeMultiplier + " / " + fields.InvestigateRangeMultiplier);
			} else if (__instance.StateInvestigate.rangeMultiplier > fields.InvestigateRangeMultiplier) {
				__instance.StateInvestigate.rangeMultiplier += fields.InvestigateRangeMultiplier;
			}
		}

		[HarmonyPatch(typeof(EnemyParent), nameof(EnemyParent.SpawnRPC))]
		[HarmonyPostfix]
		private static void PatchSpawnRPC(EnemyParent  __instance, PhotonMessageInfo _info) { //
			if (!SemiFunc.MasterOnlyRPC(_info) || !__instance.Enemy.HasStateInvestigate) return;
			var fields = ValueStorage.GetOrCreate(__instance.Enemy);
			// Logger.LogInfo(__instance.Enemy.EnemyParent.enemyName + " : " +  __instance.Enemy.StateInvestigate.rangeMultiplier + " / " + fields.InvestigateRangeMultiplier);
			fields.InvestigateRangeMultiplier = fields.MaxInvestigateRangeMultiplier;
			__instance.Enemy.StateInvestigate.rangeMultiplier = fields.MaxInvestigateRangeMultiplier;
		}
	}
}

