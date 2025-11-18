using SonicGun;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

public class ItemSoundwave : MonoBehaviour
{
	public MeshRenderer meshRenderer;

	private float startScale = 1f;

	private bool finalScale;

	// private Light lightSoundwave;
	
	internal bool hypersonic = false; // grenade stun tinnitus

	public ParticleSystem particleSystemWave;

	public ParticleSystem particleSystemSparks;

	public ParticleSystem particleSystemLightning;

	private SonicCollider _sonicCollider;

	[FormerlySerializedAs("soundExplosion")] public Sound soundSonic;
	public Sound soundHypersonic;

	public Sound soundExplosionGlobal;
	
	// public Sound tinnitusSound;
	public AudioClip tinnitusSound;
	// public AudioMixer SoundMixer;

	private void Start() {
		// Sound.CopySound(tinnitusSound, ValueStorage.tinnitusSound);
		EarSound.TinnitusSound = this.tinnitusSound;
		
		// ValueStorage.SoundMixer = SoundMixer;
		startScale = hypersonic ? base.transform.localScale.x*6 : base.transform.localScale.x;
		// lightSoundwave = GetComponentInChildren<Light>();
		_sonicCollider = GetComponentInChildren<SonicCollider>();
		_sonicCollider.gameObject.SetActive(value: hypersonic);
		meshRenderer.material.color = Color.white;
		base.transform.localScale = Vector3.zero;
		if (!hypersonic)
			soundSonic.Play(base.transform.position);
			// tinnitusSound.Play(base.transform.position);
		else {
			soundHypersonic.Play(base.transform.position, 0.75f);
			soundExplosionGlobal.Play(base.transform.position);
		}
		
		particleSystemSparks.Play();
		particleSystemLightning.Play();
		GameDirector.instance.CameraShake.ShakeDistance(8f, 3f, 8f, base.transform.position, 0.1f);
		GameDirector.instance.CameraImpact.ShakeDistance(20f, 3f, 8f, base.transform.position, 0.1f);
	}

	private void Update()
	{
		base.transform.Rotate(Vector3.up, 100f * Time.deltaTime);
		if (base.transform.localScale.x < startScale)
		{
			base.transform.localScale += Vector3.one * Time.deltaTime * 20f;
			// lightSoundwave.intensity = Mathf.Lerp(4f, 35f, Mathf.InverseLerp(0f, startScale, base.transform.localScale.x));
			// lightSoundwave.range = base.transform.localScale.x * 3f;
			return;
		}
		if (!finalScale)
		{
			base.transform.localScale = Vector3.one * startScale;
			if (hypersonic)
				_sonicCollider.gameObject.SetActive(value: false);
			finalScale = true;
			return;
		}
		float num = Mathf.Lerp(base.transform.localScale.x, startScale * 1.2f, Time.deltaTime * 2f);
		base.transform.localScale = Vector3.one * num;
		float num2 = Mathf.InverseLerp(startScale, startScale * 1.2f, num);
		Color color = meshRenderer.material.color;
		color.a = Mathf.Lerp(1f, 0f, num2);
		meshRenderer.material.color = color;
		// lightSoundwave.intensity = Mathf.Lerp(35f, 0f, num2);
		if (num2 > 0.998f)
		{
			if ((bool)particleSystemSparks)
			{
				particleSystemSparks.transform.parent = null;
			}
			Object.Destroy(base.gameObject);
		}
	}
}
