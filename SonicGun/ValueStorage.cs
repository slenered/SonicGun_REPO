using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Audio;

namespace SonicGun;

public static class ValueStorage {
	private static readonly ConditionalWeakTable<MonoBehaviour, NewValues> Data =
		new ConditionalWeakTable<MonoBehaviour, NewValues>();

	// public static Sound tinnitusSound = new Sound();
	// public static AudioMixer SoundMixer;

	public static NewValues GetOrCreate(MonoBehaviour instance)
	{
		return Data.GetOrCreateValue(instance);
	}

	// public static NewValues TryGet(MonoBehaviour instance) {
	// 	if (!Data.TryGetValue(instance, out var value)) {
	// 		value = GetOrCreate(instance);
	// 	}
	// 	return value;
	// }

	// public static void getAll() {
	// 	return Data.ToList();
	// }
}