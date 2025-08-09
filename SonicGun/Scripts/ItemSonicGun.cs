using System;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class ItemSonicGun : MonoBehaviour
{
	public enum State
	{
		Idle = 0,
		OutOfAmmo = 1,
		Buildup = 2,
		Shooting = 3,
		Reloading = 4
	}

	private PhysGrabObject physGrabObject;

	// private ItemToggle itemToggle;
	
	internal Color ChargeColor = new Color(118,118,118,1f);
	
	private bool itemHold;
	
	private bool itemHeld;

	private int charge;
	public int MaxCharges = 3;
	private int Charges;
	public Sound ChargeSound;
	public Sound OverChargeSound;
	
	private bool Overheated/* = true*/;
	private float stateRecharge = 0f;
	public float RechargeTime = 1f;
	private float RechargeTimeFlash;

	public bool hasOneShot = true;

	public float shootTime = 1f;

	public bool hasBuildUp;

	public float buildUpTime = 1f;

	private float stateCharge;
	public float ChargeUpTime = 0.75f; //2.75f;
	public float FinalChargeUpTime = 0.5f;
	private float FifthFinalChargeTime;
	public int FinalCharge = 3;

	public int numberOfBullets = 1;

	[Range(0f, 65f)]
	public float gunRandomSpread;

	public float gunRange = 50f;

	public float distanceKeep = 0.8f;

	public float gunRecoilForce = 1f;

	public float cameraShakeMultiplier = 1f;

	public float torqueMultiplier = 1f;

	public float grabStrengthMultiplier = 1f;

	public float shootCooldown = 1f;

	public float batteryDrain = 0.1f;

	public bool batteryDrainFullBar;

	public int batteryDrainFullBars = 1;

	[Range(0f, 100f)]
	public float misfirePercentageChange = 50f;

	public AnimationCurve shootLineWidthCurve;

	public float grabVerticalOffset = -0.2f;

	public float aimVerticalOffset = -5f;

	public float investigateRadius = 20f;

	private float investigateCooldown;

	public Transform gunMuzzle;

	public GameObject bulletPrefab;

	public GameObject muzzleFlashPrefab;

	public Transform gunTrigger;

	// public Transform ChargeTransform;
	public RawImage ChargeBorder;
	public RawImage ChargeBar1;
	public RawImage ChargeBar2;
	public RawImage ChargeBar3;

	internal SonicCollider SonicCollider;

	public Sound soundShoot;

	public Sound soundShootGlobal;

	public Sound soundNoAmmoClick;

	public Sound soundHit;

	private ItemBattery itemBattery;

	private PhotonView photonView;

	private PhysGrabObjectImpactDetector impactDetector;

	// private bool prevToggleState;

	private AnimationCurve triggerAnimationCurve;

	private float triggerAnimationEval;

	private bool triggerAnimationActive;

	public UnityEvent onStateIdleStart;

	public UnityEvent onStateIdleUpdate;

	public UnityEvent onStateIdleFixedUpdate;

	[Space(20f)]
	public UnityEvent onStateOutOfAmmoStart;

	public UnityEvent onStateOutOfAmmoUpdate;

	public UnityEvent onStateOutOfAmmoFixedUpdate;

	[Space(20f)]
	public UnityEvent onStateBuildupStart;

	public UnityEvent onStateBuildupUpdate;

	public UnityEvent onStateBuildupFixedUpdate;

	[Space(20f)]
	public UnityEvent onStateShootingStart;

	public UnityEvent onStateShootingUpdate;

	public UnityEvent onStateShootingFixedUpdate;

	[Space(20f)]
	public UnityEvent onStateReloadingStart;

	public UnityEvent onStateReloadingUpdate;

	public UnityEvent onStateReloadingFixedUpdate;

	private bool hasIdleUpdate = true;

	private bool hasIdleFixedUpdate = true;

	private bool hasOutOfAmmoUpdate = true;

	private bool hasOutOfAmmoFixedUpdate = true;

	private bool hasBuildupUpdate = true;

	private bool hasBuildupFixedUpdate = true;

	private bool hasShootingUpdate = true;

	private bool hasShootingFixedUpdate = true;

	private bool hasReloadingUpdate = true;

	private bool hasReloadingFixedUpdate = true;

	private RoomVolumeCheck roomVolumeCheck;

	internal float stateTimer;

	internal float stateTimeMax;

	internal State stateCurrent;

	private State statePrev;

	private bool stateStart;

	private ItemEquippable itemEquippable;


	private void Start() {
		
		// ChargeSound.Volume *= 0.3f;
		// ChargeSound.VolumeRandom *= 0.5f;
		// ChargeSound.SpatialBlend = 1f;
		//
		// OverChargeSound.Volume *= 0.2f;
		// OverChargeSound.VolumeRandom *= 0.5f;
		// OverChargeSound.SpatialBlend = 1f;
		
		FifthFinalChargeTime = FinalChargeUpTime / 5f;
		RechargeTimeFlash = RechargeTime / 6f;
		Charges = MaxCharges;
		roomVolumeCheck = GetComponent<RoomVolumeCheck>();
		itemEquippable = GetComponent<ItemEquippable>();
		physGrabObject = GetComponent<PhysGrabObject>();
		// itemToggle = GetComponent<ItemToggle>();
		itemBattery = GetComponent<ItemBattery>();
		photonView = GetComponent<PhotonView>();
		impactDetector = GetComponent<PhysGrabObjectImpactDetector>();
		triggerAnimationCurve = AssetManager.instance.animationCurveClickInOut;
		if (onStateIdleUpdate == null)
		{
			hasIdleUpdate = false;
		}
		if (onStateIdleFixedUpdate == null)
		{
			hasIdleFixedUpdate = false;
		}
		if (onStateOutOfAmmoUpdate == null)
		{
			hasOutOfAmmoUpdate = false;
		}
		if (onStateOutOfAmmoFixedUpdate == null)
		{
			hasOutOfAmmoFixedUpdate = false;
		}
		if (onStateBuildupUpdate == null)
		{
			hasBuildupUpdate = false;
		}
		if (onStateBuildupFixedUpdate == null)
		{
			hasBuildupFixedUpdate = false;
		}
		if (onStateShootingUpdate == null)
		{
			hasShootingUpdate = false;
		}
		if (onStateShootingFixedUpdate == null)
		{
			hasShootingFixedUpdate = false;
		}
		if (onStateReloadingUpdate == null)
		{
			hasReloadingUpdate = false;
		}
		if (onStateReloadingFixedUpdate == null)
		{
			hasReloadingFixedUpdate = false;
		}
	}

	private void FixedUpdate()
	{
		StateMachine(_fixedUpdate: true);
	}

	private void Update()
	{
		StateMachine(_fixedUpdate: false);
		if (physGrabObject.grabbed && physGrabObject.grabbedLocal)
		{
			PhysGrabber.instance.OverrideGrabDistance(distanceKeep);
		}
		if (triggerAnimationActive)
		{
			float num = 45f;
			triggerAnimationEval += Time.deltaTime * 4f;
			gunTrigger.localRotation = Quaternion.Euler(num * triggerAnimationCurve.Evaluate(triggerAnimationEval), 0f, 0f);
			if (triggerAnimationEval >= 1f)
			{
				gunTrigger.localRotation = Quaternion.Euler(0f, 0f, 0f);
				triggerAnimationActive = false;
				triggerAnimationEval = 1f;
			}
		}
		
		ChargeBar1.enabled = Charges >= MaxCharges;
		ChargeBar2.enabled = Charges >= MaxCharges-1;
		ChargeBar3.enabled = Charges >= MaxCharges-2;
		
		ChargeBar1.color = charge >= 1 ? Color.red : ChargeColor;
		ChargeBar2.color = charge >= 2 - (MaxCharges - Charges) ? Color.red : ChargeColor;
		ChargeBar3.color = charge >= 3 - (MaxCharges - Charges) ? Color.red : ChargeColor;

		if (itemHeld && charge == FinalCharge) {
			bool flag = FifthFinalChargeTime > stateCharge % (FifthFinalChargeTime * 2);
			ChargeBorder.color = flag ? Color.red : Color.white;
			if (flag) {
				EnemyDirector.instance.SetInvestigate(base.transform.position, 5);
			}
			// stateCharge
		} else 
			ChargeBorder.color = Overheated ? Color.black : Color.white;

		if (Charges < MaxCharges || Overheated) {
			stateCharge += Time.deltaTime;
			RestoreCharges(stateCharge);
		}
		
		
		UpdateMaster();
	}

	private void UpdateMaster()
	{
		if (!SemiFunc.IsMasterClientOrSingleplayer() || physGrabObject.playerGrabbing.Count <= 0)
		{
			return;
		}
		Quaternion turnX = Quaternion.Euler(aimVerticalOffset, 0f, 0f);
		Quaternion turnY = Quaternion.Euler(0f, 0f, 0f);
		Quaternion identity = Quaternion.identity;
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = true;
		foreach (PhysGrabber item in physGrabObject.playerGrabbing)
		{
			if (flag4)
			{
				if (item.playerAvatar.isCrouching)
				{
					flag2 = true;
				}
				if (item.playerAvatar.isCrawling)
				{
					flag3 = true;
				}
				flag4 = false;
			}
			if (item.isRotating)
			{
				flag = true;
			}
		}
		if (!flag)
		{
			physGrabObject.TurnXYZ(turnX, turnY, identity);
		}
		float num = grabVerticalOffset;
		if (flag2)
		{
			num += 0.5f;
		}
		if (flag3)
		{
			num -= 0.5f;
		}
		physGrabObject.OverrideGrabVerticalPosition(num);
		switch (flag) {
			case false when stateCurrent == State.OutOfAmmo:
				physGrabObject.OverrideTorqueStrength(0.01f);
				physGrabObject.OverrideExtraTorqueStrengthDisable();
				physGrabObject.OverrideExtraGrabStrengthDisable();
				break;
			case false: {
				if (physGrabObject.grabbed)
				{
					physGrabObject.OverrideTorqueStrength(12f);
					physGrabObject.OverrideAngularDrag(20f);
				}

				break;
			}
			case true:
				physGrabObject.OverrideTorqueStrength(2f);
				physGrabObject.OverrideAngularDrag(20f);
				break;
		}
	}

	public void Misfire() {
		if (!roomVolumeCheck.inTruck && !physGrabObject.grabbed && !physGrabObject.hasNeverBeenGrabbed && SemiFunc.IsMasterClientOrSingleplayer() && (float)Random.Range(0, 100) < misfirePercentageChange) {
			Shoot();
		}
	}

	public void Shoot() {
		if (hasOneShot) {
			if (SemiFunc.IsMultiplayer()) {
				photonView.RPC("SonicShootRPC", RpcTarget.All);
			} else {
				SonicShootRPC();
			}
			StateSet(State.Reloading);
		} else if (hasBuildUp) {
			StateSet(State.Buildup);
		} else {
			// if (SemiFunc.IsMultiplayer()) {
   //       		photonView.RPC("SonicShootRPC", RpcTarget.All);
	  //       }
	  //       else
	  //       {
   //       		SonicShootRPC();
	  //       }
			StateSet(State.Shooting);
		}
	}

	private void MuzzleFlash() {
		Object.Instantiate(muzzleFlashPrefab, gunMuzzle.position, gunMuzzle.rotation, gunMuzzle).GetComponent<ItemGunMuzzleFlash>().ActivateAllEffects();
	}

	private void StartTriggerAnimation() {
		triggerAnimationActive = true;
		triggerAnimationEval = 0f;
	}

	[PunRPC]
	public void SonicShootRPC(PhotonMessageInfo _info = default(PhotonMessageInfo)) {
		
		if (!SemiFunc.MasterOnlyRPC(_info)) {
			return;
		}
		bool hypersonic = charge > FinalCharge && itemBattery.batteryLifeInt > 0;
		stateCharge = 0f;
		if (hypersonic) {
			Charges = 0;
			charge = 0;
		}
		else {
			Charges = Math.Max(0, Charges-1);
			charge = Math.Max(0, charge-1);
		}
		
		float distanceMin = 3f * cameraShakeMultiplier;
		float distanceMax = 16f * cameraShakeMultiplier;
		SemiFunc.CameraShakeImpactDistance(gunMuzzle.position, 5f * cameraShakeMultiplier, 0.1f, distanceMin, distanceMax);
		SemiFunc.CameraShakeDistance(gunMuzzle.position, 0.1f * cameraShakeMultiplier, 0.1f * cameraShakeMultiplier, distanceMin, distanceMax);
		soundShoot.Play(gunMuzzle.position);
		soundShootGlobal.Play(gunMuzzle.position);
		MuzzleFlash();
		StartTriggerAnimation();
		if (!SemiFunc.IsMasterClientOrSingleplayer()) {
			return;
		}
		if (investigateRadius > 0f) {
			EnemyDirector.instance.SetInvestigate(base.transform.position, investigateRadius);
		}
		physGrabObject.rb.AddForceAtPosition(-gunMuzzle.forward * gunRecoilForce, gunMuzzle.position, ForceMode.Impulse);
		itemHold = false;
		itemHeld = false;
		if (hypersonic) {
			itemBattery.RemoveFullBar(1);
		}
		Vector3 endPosition = gunMuzzle.position;
		bool hit = false;
		bool flag = false;
		Vector3 vector = gunMuzzle.forward;
		if (gunRandomSpread > 0f) {
			float angle = Random.Range(0f, gunRandomSpread / 2f);
			float angle2 = Random.Range(0f, 360f);
			Vector3 normalized = Vector3.Cross(vector, Random.onUnitSphere).normalized;
			Quaternion quaternion = Quaternion.AngleAxis(angle, normalized);
			vector = (Quaternion.AngleAxis(angle2, vector) * quaternion * vector).normalized;
		}
		if (Physics.Raycast(gunMuzzle.position, vector, out var hitInfo, gunRange, (int)SemiFunc.LayerMaskGetVisionObstruct() + LayerMask.GetMask("Enemy"))) {
			endPosition = hitInfo.point;
			hit = true;
		} else {
			flag = true;
		} if (flag) {
			endPosition = gunMuzzle.position + gunMuzzle.forward * gunRange;
			hit = true;
		} 
		ShootBullet(endPosition, hit, hypersonic);
	}

	private void ShootBullet(Vector3 _endPosition, bool _hit, bool hypersonic) {
		if (SemiFunc.IsMasterClientOrSingleplayer()) {
			if (SemiFunc.IsMultiplayer()) {
				photonView.RPC("SonicShootBulletRPC", RpcTarget.All, _endPosition, _hit, hypersonic);
			} else {
				SonicShootBulletRPC(_endPosition, _hit, hypersonic);
			}
		}
	}

	[PunRPC]
	public void SonicShootBulletRPC(Vector3 _endPosition, bool _hit, bool hypersonic, PhotonMessageInfo _info = default(PhotonMessageInfo))
	{
		if (!SemiFunc.MasterOnlyRPC(_info))
		{
			return;
		}
		if (physGrabObject.playerGrabbing.Count > 1)
		{
			foreach (PhysGrabber item in physGrabObject.playerGrabbing)
			{
				item.OverrideGrabRelease();
			}
		}
		ItemGunSonicBullet component = Object.Instantiate(bulletPrefab, gunMuzzle.position, gunMuzzle.rotation).GetComponent<ItemGunSonicBullet>();
		// print(component.ToString());
		component.hitPosition = _endPosition;
		component.bulletHit = _hit;
		component.hypersonic = hypersonic;
		SonicCollider = component.GetComponentInChildren<SonicCollider>();
		soundHit.Play(_endPosition);
		component.shootLineWidthCurve = shootLineWidthCurve;
		component.ActivateAll();
	}

	private void StateSet(State _state)
	{
		if (_state == stateCurrent)
		{
			return;
		}
		if (SemiFunc.IsMultiplayer())
		{
			if (SemiFunc.IsMasterClient())
			{
				photonView.RPC("SonicStateSetRPC", RpcTarget.All, (int)_state);
			}
		}
		else
		{
			SonicStateSetRPC((int)_state);
		}
	}

	
	
	
	
	
	
	
	private void ShootLogic() {
		// print(Overheated+ " , " + Charges);
		if (physGrabObject.heldByLocalPlayer) {
			bool inputHold = SemiFunc.InputHold(InputKey.Interact);
			if (SemiFunc.IsMultiplayer()) {
				photonView.RPC("InputHoldRPC", RpcTarget.All, inputHold);
			} else {
				InputHoldRPC(inputHold);
			}
		} else if (physGrabObject.playerGrabbing.Count == 0) {
			itemHeld = false;
			itemHold =  false;
		}
		
		if (Overheated || Charges <= 0) {
			itemHeld = itemHold;
			return;
		}
		if (SemiFunc.IsMasterClientOrSingleplayer()) {
			if (itemHold != itemHeld) {
				// if (itemBattery.batteryLifeInt <= 0 && charge >= FinalCharge+1)
				// {
				// 	soundNoAmmoClick.Play(base.transform.position);
				// 	StartTriggerAnimation();
				// 	SemiFunc.CameraShakeImpact(1f, 0.1f);
				// 	physGrabObject.rb.AddForceAtPosition(-gunMuzzle.forward * 1f, gunMuzzle.position, ForceMode.Impulse);
				// }
				// else 
				if (!itemHold && itemHeld) {
					ChargeSound.PlayLoop(false, 1f, 1f);
					ChargeSound.Source.volume = 0;
					Shoot();
					// stateCharge = 0;
					// Charges = Math.Max(0, Charges-1);
					// charge = 0;
				}
			} else if (itemHold) {
				var totalCharge = ChargeUpTime * FinalCharge + FinalChargeUpTime;
				var percCharged = (ChargeUpTime * charge + stateCharge) / totalCharge;
				stateCharge += Time.deltaTime;
				ChargeSound.PlayLoop(true, 1f, 1f);
				// ChargeSound.Play(base.transform.position);
				ChargeSound.Source.volume = percCharged * 2;
				
				// physGrabObject.rb.AddForceAtPosition(Random.insideUnitSphere * (percCharged * 0.25f), physGrabObject.rb.position + Random.insideUnitSphere * 0.05f, ForceMode.Impulse);
				if (SemiFunc.IsMultiplayer()) {
					photonView.RPC("SonicStateRPC", RpcTarget.All, stateCharge);
				} else {
					SonicStateRPC(stateCharge);
				}
			
				if (stateCharge >= ChargeUpTime && charge < FinalCharge && Charges > charge) {
					stateCharge -= ChargeUpTime;
					// charge += 1;
					Changecharge(1);
					if (charge >= FinalCharge) {
						OverChargeSound.Play(base.transform.position);
					}
				} else if (stateCharge >= FinalChargeUpTime && charge == FinalCharge) {
					stateCharge = 0;
					// charge += 1;
					Changecharge(1);
				} else if (charge > FinalCharge) {
					ChargeSound.PlayLoop(false, 1f, 1f);
					ChargeSound.Source.volume = 0;
					Shoot();
					stateCharge = 0;
					SetOverheat(true);
				}
				// ChargeBar1.color = Color.red; // #767676
			}
			// prevToggleState = itemToggle.toggleState; Time.deltaTime
		}

		itemHeld = itemHold;
	}
	
	
	

	[PunRPC]
	private void InputHoldRPC(bool inputHold, PhotonMessageInfo _info = default(PhotonMessageInfo)) {
		itemHold =  inputHold;
	}
	
	[PunRPC]
	private void SonicStateRPC(float stateCh, PhotonMessageInfo _info = default(PhotonMessageInfo)) {
		stateCharge = stateCh;
		var totalCharge = ChargeUpTime * FinalCharge + FinalChargeUpTime;
		var percCharged = (ChargeUpTime * charge + stateCh) / totalCharge;
		
		physGrabObject.rb.AddForceAtPosition(Random.insideUnitSphere * (percCharged * 0.25f), physGrabObject.rb.position + Random.insideUnitSphere * 0.05f, ForceMode.Impulse);
	}

	
	private void Changecharge(int c) {
		if (SemiFunc.IsMultiplayer()) {
			photonView.RPC("ChangechargeRPC", RpcTarget.All, c);
		} else {
			ChangechargeRPC(c);
		}
	}

	[PunRPC]
	private void ChangechargeRPC(int c, PhotonMessageInfo _info = default(PhotonMessageInfo)) {
		charge = Math.Max(0, charge+c);
	}

	private void ChangeCharges(int c) {
		if (SemiFunc.IsMultiplayer()) {
			photonView.RPC("ChangeChargesRPC", RpcTarget.All, c);
		} else {
			ChangeChargesRPC(c);
		}
	}
	[PunRPC]
	private void ChangeChargesRPC(int c, PhotonMessageInfo _info = default(PhotonMessageInfo)) {
		Charges = Math.Max(0, Charges+c);
	}
	
	
	private void RestoreCharges(float c) {
		if (SemiFunc.IsMultiplayer()) {
			photonView.RPC("RestoreChargesRPC", RpcTarget.All, c);
		} else {
			RestoreChargesRPC(c);
		}
	}
	[PunRPC]
	private void RestoreChargesRPC(float c, PhotonMessageInfo _info = default(PhotonMessageInfo)) {
		if (stateCharge >= RechargeTime && Charges < MaxCharges) {
			stateCharge -= RechargeTime;
			Charges += 1;
		} else if (Overheated && Charges >= MaxCharges && stateCharge >= RechargeTime * 2) {
			stateCharge = 0;
			Overheated = false;
		}
	}
	
	private void SetOverheat(bool c) {
		if (SemiFunc.IsMultiplayer()) {
			photonView.RPC("SetOverheatRPC", RpcTarget.All, c);
		} else {
			SetOverheatRPC(c);
		}
	}
	[PunRPC]
	private void SetOverheatRPC(bool c, PhotonMessageInfo _info = default(PhotonMessageInfo)) {
		Overheated = c;
	}
	
	
	
	[PunRPC]
	private void SonicStateSetRPC(int state, PhotonMessageInfo _info = default(PhotonMessageInfo)) {
		if (SemiFunc.MasterOnlyRPC(_info)) {
			stateStart = true;
			statePrev = stateCurrent;
			stateCurrent = (State)state;
		}
	}

	private void StateMachine(bool _fixedUpdate) {
		switch (stateCurrent) {
			case State.Idle:
				StateIdle(_fixedUpdate);
				break;
			case State.OutOfAmmo:
				StateOutOfAmmo(_fixedUpdate);
				break;
			case State.Buildup:
				StateBuildup(_fixedUpdate);
				break;
			case State.Shooting:
				StateShooting(_fixedUpdate);
				break;
			case State.Reloading:
				StateReloading(_fixedUpdate);
				break;
		}
	}

	private void StateIdle(bool _fixedUpdate)
	{
		if (stateStart && !_fixedUpdate)
		{
			if (onStateIdleStart != null)
			{
				onStateIdleStart.Invoke();
			}
			stateStart = false;
		}
		if (!_fixedUpdate)
		{
			ShootLogic();
			if (hasIdleUpdate)
			{
				onStateIdleUpdate.Invoke();
			}
		}
		if (_fixedUpdate && hasIdleFixedUpdate)
		{
			onStateIdleFixedUpdate.Invoke();
		}
	}

	private void StateOutOfAmmo(bool _fixedUpdate)
	{
		if (stateStart && !_fixedUpdate)
		{
			if (onStateOutOfAmmoStart != null)
			{
				onStateOutOfAmmoStart.Invoke();
			}
			stateStart = false;
			// prevToggleState = itemToggle.toggleState;
		}
		if (!_fixedUpdate)
		{
			if (itemBattery.batteryLifeInt > 0)
			{
				StateSet(State.Idle);
				return;
			}
			ShootLogic();
			if (hasOutOfAmmoUpdate)
			{
				onStateOutOfAmmoUpdate.Invoke();
			}
		}
		if (_fixedUpdate && hasOutOfAmmoFixedUpdate)
		{
			onStateOutOfAmmoFixedUpdate.Invoke();
		}
	}

	private void StateBuildup(bool _fixedUpdate)
	{
		if (stateStart && !_fixedUpdate)
		{
			if (onStateBuildupStart != null)
			{
				onStateBuildupStart.Invoke();
			}
			stateTimer = 0f;
			stateTimeMax = buildUpTime;
			stateStart = false;
		}
		if (!_fixedUpdate)
		{
			if (hasBuildupUpdate)
			{
				onStateBuildupUpdate.Invoke();
			}
			stateTimer += Time.deltaTime;
			if ((bool)itemEquippable && itemEquippable.isEquipped)
			{
				StateSet(State.Idle);
			}
			if (stateTimer >= stateTimeMax && itemBattery.batteryLifeInt > 0)
			{
				StateSet(State.Shooting);
			}
		}
		if (_fixedUpdate && hasBuildupFixedUpdate)
		{
			onStateBuildupFixedUpdate.Invoke();
		}
	}

	private void StateShooting(bool _fixedUpdate) {
		if (stateStart && !_fixedUpdate) {
			stateStart = false;
			if (onStateShootingStart != null)
			{
				onStateShootingStart.Invoke();
			}
			if (!hasOneShot)
			{
				stateTimeMax = shootTime;
				stateTimer = stateTimeMax;
				investigateCooldown = 0f;
			}
			else
			{
				stateTimer = 0.001f;
			}
		}
		if (!_fixedUpdate) {
			stateTimer += Time.deltaTime;
			// stateCharge = 0f;
			
			if (stateTimer >= stateTimeMax) {
				// stateStart = true;
				stateTimer = 0f;
				if (SemiFunc.IsMultiplayer()) {
					photonView.RPC("SonicShootRPC", RpcTarget.All);
				}
				else
				{
					SonicShootRPC();
				}
				
				if (charge <= 0) 
					StateSet(State.Reloading);
			} 
		}
	}

	private void StateReloading(bool _fixedUpdate)
	{
		if (stateStart && !_fixedUpdate)
		{
			stateStart = false;
			if (onStateReloadingStart != null)
			{
				onStateReloadingStart.Invoke();
			}
			stateTimeMax = shootCooldown;
			stateTimer = 0f;
		}
		if (!_fixedUpdate)
		{
			stateTimer += Time.deltaTime;
			if (stateTimer >= stateTimeMax)
			{
				// if (itemBattery.batteryLifeInt > 0)
				// {
				StateSet(State.Idle);
				// }
				// else
				// {
				// 	StateSet(State.OutOfAmmo);
				// }
			}
			if (hasReloadingUpdate)
			{
				onStateReloadingUpdate.Invoke();
			}
		}
		if (_fixedUpdate && hasReloadingFixedUpdate)
		{
			onStateReloadingFixedUpdate.Invoke();
		}
	}
}
