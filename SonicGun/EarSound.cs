using UnityEngine;

namespace SonicGun;

public class EarSound {
	private static GameObject? _audioObject;
	private static AudioSource? _source;
	public static AudioClip? TinnitusSound;
	
	
	public static AudioSource PrepareSound(AudioClip clip) {
		if (_audioObject == null) {
			_audioObject = new GameObject("AudioSource");
			Object.DontDestroyOnLoad(_audioObject);
			
			_source = _audioObject.AddComponent<AudioSource>();
			_source.spatialBlend = 0f;
			_source.loop = false;
			_source.playOnAwake = false;
			_source.volume = 0f;
		}
		
		_source!.clip = clip;
		return _source;
	} 
}