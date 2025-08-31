using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Audio;

namespace SonicGun;

public static class ValueStorage {
	private static readonly ConditionalWeakTable<MonoBehaviour, NewValues> Data =
		new ConditionalWeakTable<MonoBehaviour, NewValues>();

	public static float tinnitusVolume = 0f;

	public static NewValues GetOrCreate(MonoBehaviour instance)
	{
		return Data.GetOrCreateValue(instance);
	}
}