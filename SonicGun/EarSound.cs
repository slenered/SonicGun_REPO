using UnityEngine;

namespace SonicGun;

public class EarSound {
	private static GameObject audioObject;
	private static AudioSource source;
	public static AudioClip tinnitusSound;
	
	
	public static AudioSource PrepareSound(AudioClip clip) {
		if (audioObject == null) {
			audioObject = new GameObject("AudioSource");
			Object.DontDestroyOnLoad(audioObject);
			
			source = audioObject.AddComponent<AudioSource>();
			source.spatialBlend = 0f;
			source.loop = false;
			source.playOnAwake = false;
			source.volume = 0f;
		}
		
		source.clip = clip;
		return source;
	} 
}