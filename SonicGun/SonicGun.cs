using System;
using BepInEx;
using BepInEx.Logging;
using ExitGames.Client.Photon;
using HarmonyLib;
using Photon.Pun;
using REPOLib.Modules;
using Steamworks;
using UnityEngine;
using UnityEngine.Audio;

namespace SonicGun;

[BepInPlugin("slenered.SonicGun", "SonicGun", "1.0.3")]
[BepInDependency(REPOLib.MyPluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
public class SonicGun : BaseUnityPlugin {
	internal static SonicGun Instance { get; set; } = null!;
	internal new static ManualLogSource Logger => Instance._logger;
	private ManualLogSource _logger => base.Logger;
	internal Harmony? Harmony { get; set; }

	public static NetworkedEvent TinnitusEvent = null!;
	
	private void Awake() {
		Instance = this;
		
		// Prevent the plugin from being deleted
		this.gameObject.transform.parent = null;
		this.gameObject.hideFlags = HideFlags.HideAndDontSave;

		TinnitusEvent = new NetworkedEvent("tinnitus", TinnitusEventHandler);

		PatchHarmony();
		Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
	}

	private static void TinnitusEventHandler(EventData eventData) {
		if (SemiFunc.PhotonViewIDPlayerAvatarLocal() == (int) eventData.CustomData) {
			ValueStorage.tinnitusVolume = 1f;
		}
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
	
	internal static class Patch {
		
		private static AudioSource audio = null!;

		[HarmonyPatch(typeof(GameDirector), nameof(GameDirector.OutroStart))]
		[HarmonyPostfix]
		private static void PatchGameDirectorOutroStart(GameDirector __instance) {
			ValueStorage.tinnitusVolume = 0f;
		}

		[HarmonyPatch(typeof(PlayerAvatar), nameof(PlayerAvatar.PlayerDeath))]
		[HarmonyPostfix]
		private static void PatchPlayerDeath(PlayerAvatar __instance) {
			ValueStorage.tinnitusVolume = 0f;
		}
		

		[HarmonyPatch(typeof(AudioManager), nameof(AudioManager.Update))]
		[HarmonyPrefix]
		private static bool PatchAudioManagerUpdate(AudioManager  __instance) {
			if (ValueStorage.tinnitusVolume <= 0f) {
				if (audio != null && audio.isPlaying) audio.Stop();
				return true;
			}
			ValueStorage.tinnitusVolume -= Time.deltaTime/20;
			
			if (audio == null) {
				audio = EarSound.PrepareSound(EarSound.tinnitusSound);
			}
			
			if (!audio.isPlaying) {
				audio.Play();
			}

			audio.volume = ValueStorage.tinnitusVolume*0.5f;

			float MusicVol = Mathf.Lerp(-80f, 0f, __instance.VolumeCurve.Evaluate( (float)(DataDirector.instance.SettingValueFetch(DataDirector.Setting.MusicVolume) * 0.01f) * (1.2f - ValueStorage.tinnitusVolume)));
			float SfxVol = Mathf.Lerp(-80f, 0f, __instance.VolumeCurve.Evaluate((float)(DataDirector.instance.SettingValueFetch(DataDirector.Setting.SfxVolume) * 0.01f) * (1.2f - ValueStorage.tinnitusVolume)));
			float VoiceVol = Mathf.Lerp(-80f, 0f, __instance.VolumeCurve.Evaluate((float)(DataDirector.instance.SettingValueFetch(DataDirector.Setting.ProximityVoice) * 0.01f) * (1.2f - ValueStorage.tinnitusVolume)));
			float TTSVol = Mathf.Lerp(-80f, 0f, __instance.VolumeCurve.Evaluate((float)(DataDirector.instance.SettingValueFetch(DataDirector.Setting.TextToSpeechVolume) * 0.01f) * (1.2f - ValueStorage.tinnitusVolume)));
			
			__instance.MusicMasterGroup.audioMixer.SetFloat("MusicVolume", MusicVol);
			__instance.SoundMasterGroup.audioMixer.SetFloat("SoundVolume", SfxVol);
			__instance.MicrophoneSoundGroup.audioMixer.SetFloat("MicrophoneVolume", VoiceVol);
			__instance.TTSSoundGroup.audioMixer.SetFloat("TTSVolume", TTSVol);
			
			return false;
		}
		[HarmonyPatch(typeof(ReverbDirector), nameof(ReverbDirector.Update))]
      [HarmonyPrefix]
		private static bool PatchReverbDirectorUpdate(ReverbDirector __instance) {
			if (ValueStorage.tinnitusVolume <= 0f) {
				return true;
			}
			if (PlayerController.instance.playerAvatarScript.RoomVolumeCheck.CurrentRooms.Count > 0) {
				ReverbPreset reverbPreset = ScriptableObject.CreateInstance<ReverbPreset>();
				ReverbPreset roomReverbPreset = PlayerController.instance.playerAvatarScript.RoomVolumeCheck.CurrentRooms[0].ReverbPreset;
				
				reverbPreset.dryLevel = Mathf.Lerp(roomReverbPreset.dryLevel, 0f, ValueStorage.tinnitusVolume);
				reverbPreset.room = Mathf.Lerp(roomReverbPreset.room, -100f, ValueStorage.tinnitusVolume);
				reverbPreset.roomHF = Mathf.Lerp(roomReverbPreset.roomHF, -1000f, ValueStorage.tinnitusVolume);
				reverbPreset.decayTime = Mathf.Lerp(roomReverbPreset.decayTime, 1f, ValueStorage.tinnitusVolume);
				reverbPreset.decayHFRatio = Mathf.Lerp(roomReverbPreset.decayHFRatio, 0.83f, ValueStorage.tinnitusVolume);
				reverbPreset.reflections = Mathf.Lerp(roomReverbPreset.reflections, 1646f, ValueStorage.tinnitusVolume);
				reverbPreset.reflectDelay = Mathf.Lerp(roomReverbPreset.reflectDelay, 0.3f, ValueStorage.tinnitusVolume);
				reverbPreset.reverb = Mathf.Lerp(roomReverbPreset.reverb, 2000f, ValueStorage.tinnitusVolume);
				reverbPreset.reverbDelay = Mathf.Lerp(roomReverbPreset.reverbDelay, 0.1f, ValueStorage.tinnitusVolume);
				reverbPreset.diffusion = Mathf.Lerp(roomReverbPreset.diffusion, 10f, ValueStorage.tinnitusVolume);
				reverbPreset.density = Mathf.Lerp(roomReverbPreset.density, 100f, ValueStorage.tinnitusVolume);
				reverbPreset.hfReference = Mathf.Lerp(roomReverbPreset.hfReference, 5000f, ValueStorage.tinnitusVolume);
				reverbPreset.roomLF = Mathf.Lerp(roomReverbPreset.roomLF, -28f, ValueStorage.tinnitusVolume);
				reverbPreset.lfReference = Mathf.Lerp(roomReverbPreset.lfReference, 250f, ValueStorage.tinnitusVolume);
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
			var fields = ValueStorage.GetOrCreate(__instance);
			fields.MaxInvestigateRangeMultiplier = __instance.StateInvestigate.rangeMultiplier;
			fields.InvestigateRangeMultiplier = fields.MaxInvestigateRangeMultiplier;
		}

		[HarmonyPatch(typeof(Enemy), nameof(Enemy.Update))]
		[HarmonyPostfix]
		private static void PatchUpdate(Enemy __instance) {
			if (!__instance.HasStateInvestigate) return;
			var fields = ValueStorage.GetOrCreate(__instance);
			if (__instance.StateInvestigate.rangeMultiplier < fields.InvestigateRangeMultiplier) {
				__instance.StateInvestigate.rangeMultiplier += (fields.InvestigateRangeMultiplier / 180)*Time.deltaTime;
			} else if (__instance.StateInvestigate.rangeMultiplier > fields.InvestigateRangeMultiplier) {
				__instance.StateInvestigate.rangeMultiplier += fields.InvestigateRangeMultiplier;
			}
		}

		[HarmonyPatch(typeof(EnemyParent), nameof(EnemyParent.SpawnRPC))]
		[HarmonyPostfix]
		private static void PatchSpawnRPC(EnemyParent  __instance, PhotonMessageInfo _info) { //
			if (!SemiFunc.MasterOnlyRPC(_info) || !__instance.Enemy.HasStateInvestigate) return;
			var fields = ValueStorage.GetOrCreate(__instance.Enemy);
			fields.InvestigateRangeMultiplier = fields.MaxInvestigateRangeMultiplier;
			__instance.Enemy.StateInvestigate.rangeMultiplier = fields.MaxInvestigateRangeMultiplier;
		}
	}
}

