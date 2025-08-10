using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using SonicGun;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class SonicCollider : MonoBehaviour
{
	public enum BreakImpact
	{
		None = 0,
		Light = 1,
		Medium = 2,
		Heavy = 3
	}

	public enum TorqueAxis
	{
		up = 0,
		down = 1,
		left = 2,
		right = 3,
		forward = 4,
		back = 5
	}

	public enum HitType
	{
		Player = 0,
		PhysObject = 1,
		Enemy = 2
	}

	public class Hit
	{
		public HitType hitType;

		public GameObject hitObject;

		public float cooldown;
	}
    
    // internal bool hypersonic = false;

	public bool playerLogic = true;

	[Space]
	private bool playerKill = false;

	private int playerDamage = 0;

	private float playerDamageCooldown = 0.25f;

	public float playerHitForce;

	public bool playerRayCast;

	public float playerTumbleForce;

	public float playerTumbleTorque;

	public TorqueAxis playerTumbleTorqueAxis = TorqueAxis.down;

	public float playerTumbleTime;

	public float playerTumbleImpactHurtTime;

	public int playerTumbleImpactHurtDamage;

	public bool physLogic = true;

	[Space]
	private bool physDestroy = false;

	private bool physHingeDestroy = false;

	private bool physHingeBreak = false;

	private BreakImpact physImpact = BreakImpact.None;

	private float physDamageCooldown = 0.25f;

	public float physHitForce;

	public float physHitTorque;

	public bool physRayCast;

	public bool enemyLogic = true;

	public Enemy enemyHost;

	[Space]
	[FormerlySerializedAs("enemyDespawn")]
	private bool enemyKill = false;

	public bool enemyStun = true;

	public float enemyStunTime = 2f;

	public EnemyType enemyStunType = EnemyType.Medium;

	public float enemyFreezeTime = 0.1f;

	[Space]
	public BreakImpact enemyImpact = BreakImpact.Medium;

	private int enemyDamage = 0;

	private float enemyDamageCooldown = 0.25f;

	public float enemyHitForce;

	public float enemyHitTorque;

	public bool enemyRayCast;

	public bool enemyHitTriggers = true;

	public bool deathPit;

	[Range(0f, 180f)]
	public float hitSpread = 180f;

	public List<PhysGrabObject> ignoreObjects = new List<PhysGrabObject>();

	internal bool ignoreLocalPlayer;

	public bool hasTimer;

	public float timer = 0.2f;

	public bool destroyOnTimerEnd;

	public bool hasCustomRaycastPosition;

	public Vector3 customRaycastPosition;

	private float timerOriginal;

	public UnityEvent onImpactAny;

	public UnityEvent onImpactPlayer;

	internal PlayerAvatar onImpactPlayerAvatar;

	public UnityEvent onImpactPhysObject;

	public UnityEvent onImpactEnemy;

	internal Enemy onImpactEnemyEnemy;

	private Collider Collider;

	private BoxCollider BoxCollider;

	private SphereCollider SphereCollider;

	private bool ColliderIsBox = true;

	private LayerMask LayerMask;

	private LayerMask RayMask;

	internal List<Hit> hits = new List<Hit>();

	private bool colliderCheckRunning;

	private bool cooldownLogicRunning;

	private Vector3 applyForce;

	private Vector3 applyTorque;

	private void Awake() {
		BoxCollider = GetComponent<BoxCollider>();
		if (!BoxCollider)
		{
			SphereCollider = GetComponent<SphereCollider>();
			Collider = SphereCollider;
			ColliderIsBox = false;
		}
		else
		{
			Collider = BoxCollider;
		}
		Collider.isTrigger = true;
		timerOriginal = timer;
		LayerMask = (int)SemiFunc.LayerMaskGetPhysGrabObject() + LayerMask.GetMask("Player") + LayerMask.GetMask("Default") + LayerMask.GetMask("Enemy");
		RayMask = LayerMask.GetMask("Default", "PhysGrabObjectHinge");
	}

	private void OnEnable()
	{
		if (!colliderCheckRunning)
		{
			colliderCheckRunning = true;
			StartCoroutine(ColliderCheck());
		}
	}

	private void OnDisable()
	{
		StopAllCoroutines();
		colliderCheckRunning = false;
		cooldownLogicRunning = false;
		hits.Clear();
	}

	private void HasTimerLogic()
	{
		if (!hasTimer)
		{
			return;
		}
		if (timer <= 0f)
		{
			if (destroyOnTimerEnd)
			{
				Object.Destroy(base.gameObject);
			}
			else
			{
				base.gameObject.SetActive(value: false);
				timer = timerOriginal;
			}
		}
		if (timer > 0f)
		{
			timer -= Time.deltaTime;
		}
	}

	private IEnumerator CooldownLogic()
	{
		while (hits.Count > 0)
		{
			for (int i = 0; i < hits.Count; i++)
			{
				Hit hit = hits[i];
				hit.cooldown -= Time.deltaTime;
				if (hit.cooldown <= 0f)
				{
					hits.RemoveAt(i);
					i--;
				}
			}
			yield return null;
		}
		cooldownLogicRunning = false;
	}

	private bool CanHit(GameObject hitObject, float cooldown, bool raycast, Vector3 hitPosition, HitType hitType)
	{
		foreach (Hit hit2 in hits)
		{
			if (hit2.hitObject == hitObject)
			{
				return false;
			}
		}
		Hit hit = new Hit();
		hit.hitObject = hitObject;
		hit.cooldown = cooldown;
		hit.hitType = hitType;
		hits.Add(hit);
		if (!cooldownLogicRunning)
		{
			StartCoroutine(CooldownLogic());
			cooldownLogicRunning = true;
		}
		if (raycast)
		{
			Vector3 vector = Collider.bounds.center;
			if (hasCustomRaycastPosition)
			{
				vector = base.transform.TransformPoint(customRaycastPosition);
			}
			Vector3 normalized = (hitPosition - vector).normalized;
			float maxDistance = Vector3.Distance(hitPosition, vector);
			RaycastHit[] array = Physics.RaycastAll(vector, normalized, maxDistance, RayMask, QueryTriggerInteraction.Collide);
			for (int i = 0; i < array.Length; i++)
			{
				RaycastHit raycastHit = array[i];
				if (raycastHit.collider.gameObject.CompareTag("Wall"))
				{
					PhysGrabObject componentInParent = hitObject.GetComponentInParent<PhysGrabObject>();
					PhysGrabObject componentInParent2 = raycastHit.collider.gameObject.GetComponentInParent<PhysGrabObject>();
					if (!componentInParent || componentInParent != componentInParent2)
					{
						return false;
					}
				}
			}
		}
		return true;
	}

	private IEnumerator ColliderCheck() {
		yield return null;
		while (!LevelGenerator.Instance || !LevelGenerator.Instance.Generated) {
			yield return new WaitForSeconds(0.1f);
		}
		while (true) {
			Collider[] array;
			if (ColliderIsBox) {
				Vector3 center = base.transform.TransformPoint(BoxCollider.center);
				Vector3 vector = new Vector3(base.transform.lossyScale.x * BoxCollider.size.x, base.transform.lossyScale.y * BoxCollider.size.y, base.transform.lossyScale.z * BoxCollider.size.z);
				array = Physics.OverlapBox(center, vector * 0.5f, base.transform.rotation, LayerMask, QueryTriggerInteraction.Collide);
			} else {
				Vector3 center2 = Collider.bounds.center;
				float radius = base.transform.lossyScale.x * SphereCollider.radius;
				array = Physics.OverlapSphere(center2, radius, LayerMask, QueryTriggerInteraction.Collide);
			}
			if (array.Length != 0) {
				Collider[] array2 = array;
				foreach (Collider collider in array2) {
					if (playerLogic && playerDamageCooldown > 0f && collider.gameObject.CompareTag("Player")) {
						PlayerAvatar playerAvatar = collider.gameObject.GetComponentInParent<PlayerAvatar>();
						if (!playerAvatar) {
							PlayerController componentInParent = collider.gameObject.GetComponentInParent<PlayerController>();
							if ((bool)componentInParent) {
								playerAvatar = componentInParent.playerAvatarScript;
							}
						}
						if ((bool)playerAvatar) {
							PlayerHurt(playerAvatar);
						}
					}
					if (!(enemyDamageCooldown > 0f) && !(physDamageCooldown > 0f) && !(playerDamageCooldown > 0f))
					{
						continue;
					}
					if (collider.gameObject.CompareTag("Phys Grab Object"))
					{
						PhysGrabObject componentInParent2 = collider.gameObject.GetComponentInParent<PhysGrabObject>();
						if (ignoreObjects.Contains(componentInParent2) || !componentInParent2 || (componentInParent2.impactDetector.isCart && collider.transform.parent.gameObject.name != "Phys Object Collider"))
						{
							continue;
						}
						bool flag = false;
						PlayerTumble componentInParent3 = collider.gameObject.GetComponentInParent<PlayerTumble>();
						if ((bool)componentInParent3)
						{
							flag = true;
						}
						if (playerLogic && playerDamageCooldown > 0f && flag)
						{
							PlayerHurt(componentInParent3.playerAvatar);
						}
						if (!SemiFunc.IsMasterClientOrSingleplayer())
						{
							continue;
						}
						EnemyRigidbody enemyRigidbody = null;
						if (enemyLogic && !flag)
						{
							enemyRigidbody = collider.gameObject.GetComponentInParent<EnemyRigidbody>();
							EnemyHurtRigidbody(enemyRigidbody, componentInParent2);
						}
						if (!physLogic || (bool)enemyRigidbody || flag || !(physDamageCooldown > 0f) || !CanHit(componentInParent2.gameObject, physDamageCooldown, physRayCast, componentInParent2.centerPoint, HitType.PhysObject))
						{
							continue;
						}
						bool flag2 = false;
						PhysGrabObjectImpactDetector componentInParent4 = collider.gameObject.GetComponentInParent<PhysGrabObjectImpactDetector>();
						if ((bool)componentInParent4)
						{
							if (physHingeDestroy)
							{
								PhysGrabHinge component = componentInParent2.GetComponent<PhysGrabHinge>();
								if ((bool)component)
								{
									component.DestroyHinge();
									flag2 = true;
								}
							}
							else if (physHingeBreak)
							{
								PhysGrabHinge component2 = componentInParent2.GetComponent<PhysGrabHinge>();
								if ((bool)component2 && (bool)component2.joint)
								{
									component2.joint.breakForce = 0f;
									component2.joint.breakTorque = 0f;
									flag2 = true;
								}
							}
							if (!flag2)
							{
								if (physDestroy)
								{
									if (!componentInParent4.destroyDisable)
									{
										PhysGrabHinge component3 = componentInParent2.GetComponent<PhysGrabHinge>();
										if ((bool)component3)
										{
											component3.DestroyHinge();
										}
										else
										{
											componentInParent4.DestroyObject();
										}
									}
									else
									{
										PhysObjectHurt(componentInParent2, BreakImpact.Heavy, 50f, 30f, apply: true, destroyLaunch: true);
									}
									flag2 = true;
								}
								else if ((bool)componentInParent2 && PhysObjectHurt(componentInParent2, physImpact, physHitForce, physHitTorque, apply: true, destroyLaunch: false))
								{
									flag2 = true;
								}
							}
						}
						if (flag2)
						{
							onImpactAny.Invoke();
							onImpactPhysObject.Invoke();
						}
					}
					else
					{
						if (!SemiFunc.IsMasterClientOrSingleplayer() || !enemyLogic)
						{
							continue;
						}
						Enemy componentInParent5 = collider.gameObject.GetComponentInParent<Enemy>();
						if ((bool)componentInParent5 && !componentInParent5.HasRigidbody && CanHit(componentInParent5.gameObject, enemyDamageCooldown, enemyRayCast, componentInParent5.transform.position, HitType.Enemy) && EnemyHurt(componentInParent5))
						{
							onImpactAny.Invoke();
							onImpactEnemyEnemy = componentInParent5;
							onImpactEnemy.Invoke();
						}
						if (!enemyHitTriggers)
						{
							continue;
						}
						EnemyParent componentInParent6 = collider.gameObject.GetComponentInParent<EnemyParent>();
						if ((bool)componentInParent6)
						{
							EnemyRigidbody componentInChildren = componentInParent6.GetComponentInChildren<EnemyRigidbody>();
							if ((bool)componentInChildren)
							{
								EnemyHurtRigidbody(componentInChildren, componentInChildren.physGrabObject);
							}
						}
					}
				}
			}
			yield return new WaitForSeconds(0.05f);
		}
	}

	private void EnemyHurtRigidbody(EnemyRigidbody _enemyRigidbody, PhysGrabObject _physGrabObject)
	{
		if (enemyDamageCooldown > 0f && (bool)_enemyRigidbody && CanHit(_physGrabObject.gameObject, enemyDamageCooldown, enemyRayCast, _physGrabObject.centerPoint, HitType.Enemy) && EnemyHurt(_enemyRigidbody.enemy))
		{
			onImpactAny.Invoke();
			onImpactEnemyEnemy = _enemyRigidbody.enemy;
			onImpactEnemy.Invoke();
		}
	}

	private bool EnemyHurt(Enemy _enemy)
	{
		if (_enemy == enemyHost)
		{
			return false;
		}
		if (!enemyLogic)
		{
			return false;
		}

		// print("EnemyHit: " + _enemy.EnemyParent);
		if (_enemy.HasStateInvestigate) {
			_enemy.StateInvestigate.rangeMultiplier = 0;
			var fields = ValueStorage.GetOrCreate(_enemy);
			if (_enemy.EnemyParent.enemyName == "Huntsman")
				fields.InvestigateRangeMultiplier /= 4;
			else
				fields.InvestigateRangeMultiplier /= 2;
		}
		
		
		if (enemyStun && _enemy.HasStateStunned && _enemy.Type <= enemyStunType)
		{
			_enemy.StateStunned.Set(enemyStunTime);
		}
		if (enemyFreezeTime > 0f)
		{
			_enemy.Freeze(enemyFreezeTime);
		}
		if (_enemy.HasRigidbody)
		{
			bool apply = !(enemyFreezeTime > 0f);
			PhysObjectHurt(_enemy.Rigidbody.physGrabObject, enemyImpact, enemyHitForce, enemyHitTorque, apply, destroyLaunch: false);
			if (enemyFreezeTime > 0f)
			{
				_enemy.Rigidbody.FreezeForces(applyForce, applyTorque);
			}
		}
		
		// GameObject EnemyController = _enemy.EnemyParent.EnableObject.transform.Find("Controller").gameObject;
		// EnemyStateInvestigate EnemyInvestigate = EnemyController.GetComponent<EnemyStateInvestigate>();
		// EnemyInvestigate.
		
		
		// bool flag = false;
		// if (deathPit)
		// {
		// 	if (SemiFunc.MoonLevel() < 2 || !_enemy.HasHealth)
		// 	{
		// 		enemyKill = true;
		// 	}
		// 	else
		// 	{
		// 		enemyKill = false;
		// 		if (_enemy.Health.deathPitCooldown <= 0f)
		// 		{
		// 			int damage = 120;
		// 			if (SemiFunc.MoonLevel() == 3)
		// 			{
		// 				damage = 80;
		// 			}
		// 			else if (SemiFunc.MoonLevel() >= 4)
		// 			{
		// 				damage = 40;
		// 			}
		// 			_enemy.Health.Hurt(damage, base.transform.forward);
		// 			_enemy.Health.DeathPitCooldown();
		// 			_enemy.StateStunned.Set(5f);
		// 			if (_enemy.HasRigidbody)
		// 			{
		// 				PhysObjectHurt(_enemy.Rigidbody.physGrabObject, BreakImpact.Heavy, 0.1f, 0.1f, apply: false, destroyLaunch: true, _enemy);
		// 				_enemy.Rigidbody.FreezeForces(applyForce * 10f, applyTorque * 10f);
		// 				_enemy.Freeze(0.2f);
		// 			}
		// 		}
		// 	}
		// }
		// if (enemyKill)
		// {
		// 	if (_enemy.HasHealth)
		// 	{
		// 		_enemy.Health.Hurt(_enemy.Health.healthCurrent, base.transform.forward);
		// 	}
		// 	else if (_enemy.HasStateDespawn)
		// 	{
		// 		_enemy.EnemyParent.SpawnedTimerSet(0f);
		// 		_enemy.CurrentState = EnemyState.Despawn;
		// 		flag = true;
		// 	}
		// }
		// if (!flag && !deathPit)
		// {
		
		// 	if (enemyDamage > 0 && _enemy.HasHealth)
		// 	{
		// 		_enemy.Health.Hurt(enemyDamage, applyForce.normalized);
		// 	}
		// }
		return true;
	}

	private void Update()
	{
		HasTimerLogic();
	}

	private void tinnitus(PlayerAvatar _player) {
		if (SemiFunc.IsMultiplayer()) {
			print("Multiplayer");
			SonicGun.SonicGun.tinnitusEvent.RaiseEvent(_player.photonView.ViewID, REPOLib.Modules.NetworkingEvents.RaiseAll, SendOptions.SendReliable);
		} else {
			print("singleplayer");
			ValueStorage.tinnitusVolume = 1f;
		}
	}
	
	private void PlayerHurt(PlayerAvatar _player) {
		if (ignoreLocalPlayer && _player.isLocal) {
			ignoreLocalPlayer = false;
		} else {
			if (GameManager.Multiplayer() && !_player.photonView.IsMine)
			{
				return;
			}
			
			tinnitus(_player);
			
			int enemyIndex = SemiFunc.EnemyGetIndex(enemyHost);
			if (playerKill)
			{
				onImpactAny.Invoke();
				onImpactPlayer.Invoke();
				_player.playerHealth.Hurt(_player.playerHealth.health, savingGrace: true, enemyIndex);
			}
			else
			{
				if (!CanHit(_player.gameObject, playerDamageCooldown, playerRayCast, _player.PlayerVisionTarget.VisionTransform.position, HitType.Player))
				{
					return;
				}
				_player.playerHealth.Hurt(playerDamage, savingGrace: true, enemyIndex);
				bool flag = false;
				Vector3 center = Collider.bounds.center;
				Vector3 normalized = (_player.PlayerVisionTarget.VisionTransform.position - center).normalized;
				normalized = SemiFunc.ClampDirection(normalized, base.transform.forward, hitSpread);
				bool flag2 = _player.tumble.isTumbling;
				if (playerTumbleTime > 0f && _player.playerHealth.health > 0)
				{
					_player.tumble.TumbleRequest(_isTumbling: true, _playerInput: false);
					_player.tumble.TumbleOverrideTime(playerTumbleTime);
					if (playerTumbleImpactHurtTime > 0f)
					{
						_player.tumble.ImpactHurtSet(playerTumbleImpactHurtTime, playerTumbleImpactHurtDamage);
					}
					flag2 = true;
					flag = true;
				}
				if (flag2 && (playerTumbleForce > 0f || playerTumbleTorque > 0f))
				{
					flag = true;
					if (playerTumbleForce > 0f)
					{
						_player.tumble.TumbleForce(normalized * playerTumbleForce);
					}
					if (playerTumbleTorque > 0f)
					{
						Vector3 rhs = Vector3.zero;
						if (playerTumbleTorqueAxis == TorqueAxis.up)
						{
							rhs = _player.transform.up;
						}
						if (playerTumbleTorqueAxis == TorqueAxis.down)
						{
							rhs = -_player.transform.up;
						}
						if (playerTumbleTorqueAxis == TorqueAxis.right)
						{
							rhs = _player.transform.right;
						}
						if (playerTumbleTorqueAxis == TorqueAxis.left)
						{
							rhs = -_player.transform.right;
						}
						if (playerTumbleTorqueAxis == TorqueAxis.forward)
						{
							rhs = _player.transform.forward;
						}
						if (playerTumbleTorqueAxis == TorqueAxis.back)
						{
							rhs = -_player.transform.forward;
						}
						Vector3 torque = Vector3.Cross((_player.localCameraPosition - center).normalized, rhs) * playerTumbleTorque;
						_player.tumble.TumbleTorque(torque);
					}
				}
				if (!flag2 && playerHitForce > 0f)
				{
					PlayerController.instance.ForceImpulse(normalized * playerHitForce);
				}
				if (playerHitForce > 0f || playerDamage > 0 || flag)
				{
					onImpactPlayerAvatar = _player;
					onImpactAny.Invoke();
					onImpactPlayer.Invoke();
				}
			}
		}
	}
	
	

	private bool PhysObjectHurt(PhysGrabObject physGrabObject, BreakImpact impact, float hitForce, float hitTorque, bool apply, bool destroyLaunch, Enemy enemy = null)
	{
		bool result = false;
		switch (impact)
		{
		case BreakImpact.Light:
			physGrabObject.lightBreakImpulse = true;
			result = true;
			break;
		case BreakImpact.Medium:
			physGrabObject.mediumBreakImpulse = true;
			result = true;
			break;
		case BreakImpact.Heavy:
			physGrabObject.heavyBreakImpulse = true;
			result = true;
			break;
		}
		if ((bool)enemyHost && impact != 0 && physGrabObject.playerGrabbing.Count <= 0 && !physGrabObject.impactDetector.isEnemy)
		{
			physGrabObject.impactDetector.enemyInteractionTimer = 2f;
		}
		if (hitForce > 0f)
		{
			if (hitForce >= 5f && physGrabObject.playerGrabbing.Count > 0 && !physGrabObject.overrideKnockOutOfGrabDisable)
			{
				foreach (PhysGrabber item in physGrabObject.playerGrabbing.ToList())
				{
					if (!SemiFunc.IsMultiplayer())
					{
						item.ReleaseObjectRPC(physGrabEnded: true, 2f);
						continue;
					}
					item.photonView.RPC("ReleaseObjectRPC", RpcTarget.All, false, 1f);
				}
			}
			Vector3 center = Collider.bounds.center;
			Vector3 normalized = (physGrabObject.centerPoint - center).normalized;
			normalized = SemiFunc.ClampDirection(normalized, base.transform.forward, hitSpread);
			applyForce = normalized * hitForce;
			Vector3 normalized2 = (physGrabObject.centerPoint - center).normalized;
			Vector3 rhs = -physGrabObject.transform.up;
			applyTorque = Vector3.Cross(normalized2, rhs) * hitTorque;
			if (destroyLaunch && !physGrabObject.rb.isKinematic)
			{
				physGrabObject.rb.velocity = Vector3.zero;
				physGrabObject.rb.angularVelocity = Vector3.zero;
				physGrabObject.impactDetector.destroyDisableLaunches++;
				physGrabObject.impactDetector.destroyDisableLaunchesTimer = 10f;
				float num = 20f;
				if ((bool)enemy)
				{
					num = 3f;
				}
				Vector3 vector = Random.insideUnitSphere.normalized * 4f;
				if (physGrabObject.impactDetector.destroyDisableLaunches >= 3)
				{
					vector *= num;
					physGrabObject.impactDetector.destroyDisableLaunches = 0;
				}
				vector.y = 0f;
				if ((bool)enemy)
				{
					vector *= 0.25f;
				}
				applyForce = (Vector3.up * num + vector) * physGrabObject.rb.mass;
				applyTorque = Random.insideUnitSphere.normalized * 0.25f * physGrabObject.rb.mass;
				physGrabObject.DeathPitEffectCreate();
			}
			if (apply)
			{
				physGrabObject.rb.AddForce(applyForce, ForceMode.Impulse);
				physGrabObject.rb.AddTorque(applyTorque, ForceMode.Impulse);
				result = true;
			}
		}
		return result;
	}

	private void OnDrawGizmos()
	{
		BoxCollider component = GetComponent<BoxCollider>();
		SphereCollider component2 = GetComponent<SphereCollider>();
		if ((bool)component2 && (base.transform.localScale.z != base.transform.localScale.x || base.transform.localScale.z != base.transform.localScale.y))
		{
			Debug.LogError("Sphere Collider must be uniform scale: " + base.transform.localScale.ToString(), base.transform.gameObject);
		}
		Gizmos.color = new Color(1f, 0f, 0.39f, 6f);
		Gizmos.matrix = base.transform.localToWorldMatrix;
		if ((bool)component)
		{
			Gizmos.DrawWireCube(component.center, component.size);
		}
		if ((bool)component2)
		{
			Gizmos.DrawWireSphere(component2.center, component2.radius);
		}
		Gizmos.color = new Color(1f, 0f, 0.39f, 0.2f);
		if ((bool)component)
		{
			Gizmos.DrawCube(component.center, component.size);
		}
		if ((bool)component2)
		{
			Gizmos.DrawSphere(component2.center, component2.radius);
		}
		Gizmos.color = Color.white;
		Gizmos.matrix = Matrix4x4.identity;
		Vector3 vector = Vector3.zero;
		if ((bool)component)
		{
			vector = component.bounds.center;
		}
		if ((bool)component2)
		{
			vector = component2.bounds.center;
		}
		Vector3 vector2 = vector + base.transform.forward * 0.5f;
		Gizmos.DrawLine(vector, vector2);
		Gizmos.DrawLine(vector2, vector2 + Vector3.LerpUnclamped(-base.transform.forward, -base.transform.right, 0.5f) * 0.25f);
		Gizmos.DrawLine(vector2, vector2 + Vector3.LerpUnclamped(-base.transform.forward, base.transform.right, 0.5f) * 0.25f);
		if (hitSpread < 180f)
		{
			Gizmos.color = new Color(1f, 1f, 1f, 0.2f);
			Vector3 vector3 = (Quaternion.AngleAxis(hitSpread, base.transform.right) * base.transform.forward).normalized * 1.5f;
			Vector3 vector4 = (Quaternion.AngleAxis(0f - hitSpread, base.transform.right) * base.transform.forward).normalized * 1.5f;
			Vector3 vector5 = (Quaternion.AngleAxis(hitSpread, base.transform.up) * base.transform.forward).normalized * 1.5f;
			Vector3 vector6 = (Quaternion.AngleAxis(0f - hitSpread, base.transform.up) * base.transform.forward).normalized * 1.5f;
			Gizmos.DrawRay(vector, vector3);
			Gizmos.DrawRay(vector, vector4);
			Gizmos.DrawRay(vector, vector5);
			Gizmos.DrawRay(vector, vector6);
			Gizmos.DrawLineStrip(new Vector3[4]
			{
				vector + vector3,
				vector + vector5,
				vector + vector4,
				vector + vector6
			}, looped: true);
		}
		else if (hitSpread > 180f)
		{
			Debug.LogError("Hit Spread cannot be greater than 180 degrees");
		}
		if (hasCustomRaycastPosition)
		{
			Gizmos.color = Color.blue;
			Gizmos.DrawWireSphere(base.transform.TransformPoint(customRaycastPosition), 0.2f);
		}
	}
}
