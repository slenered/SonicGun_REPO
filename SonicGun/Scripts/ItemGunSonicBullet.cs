using System.Collections;
using UnityEngine;

public class ItemGunSonicBullet : MonoBehaviour
{
	private Transform hitEffectTransform;

	private ParticleSystem particleSparks;

	private ParticleSystem particleSmoke;

	private ParticleSystem particleImpact;

	private Light hitLight;

	private LineRenderer shootLine;

	public bool hasHurtCollider = false;

	public SonicCollider sonicCollider;

	public float investigateRadius = 15;

	internal bool bulletHit;

	internal Vector3 hitPosition;
	
	internal bool hypersonic;

	public float hurtColliderTimer = 0.25f;

	private bool shootLineActive;

	private float shootLineLerp;

	internal AnimationCurve shootLineWidthCurve;

	public GameObject hitGameObject;

	public float hitGameObjectDestroyTime = 2f;

	public bool hasExtraParticles;

	public GameObject extraParticles;

	public void ActivateAll() {
		base.gameObject.SetActive(value: true);
		hitEffectTransform = base.transform.Find("Hit Effect");
		particleSparks = hitEffectTransform.Find("Particle Sparks").GetComponent<ParticleSystem>();
		particleSmoke = hitEffectTransform.Find("Particle Smoke").GetComponent<ParticleSystem>();
		particleImpact = hitEffectTransform.Find("Particle Impact").GetComponent<ParticleSystem>();
		hitLight = hitEffectTransform.Find("Hit Light").GetComponent<Light>();
		shootLine = GetComponentInChildren<LineRenderer>();
		Vector3 position = base.transform.position;
		Vector3 forward = hitPosition - position;
		shootLine.enabled = true;
		shootLine.SetPosition(0, base.transform.position);
		shootLine.SetPosition(1, base.transform.position + forward.normalized * 0.5f);
		shootLine.SetPosition(2, hitPosition - forward.normalized * 0.5f);
		shootLine.SetPosition(3, hitPosition);
		shootLineActive = true;
		shootLineLerp = 0f;
		if (bulletHit) {
			hitEffectTransform.gameObject.SetActive(value: true);
			particleSparks.gameObject.SetActive(value: true);
			particleSmoke.gameObject.SetActive(value: true);
			particleImpact.gameObject.SetActive(value: true);
			hitLight.enabled = true;
			GameObject gameObject = hitGameObject;
			if (hasHurtCollider) {
				gameObject = sonicCollider.gameObject;
			}
			gameObject.gameObject.SetActive(value: true);
			gameObject.GetComponent<ItemSoundwave>().hypersonic = hypersonic;
			Quaternion rotation = Quaternion.LookRotation(forward);
			gameObject.transform.rotation = rotation;
			gameObject.transform.position = hitPosition;
			hitEffectTransform.position = hitPosition;
			hitEffectTransform.rotation = rotation;
			if (hasExtraParticles) {
				extraParticles.SetActive(value: true);
				extraParticles.transform.position = hitPosition;
				extraParticles.transform.rotation = rotation;
			}
			if (investigateRadius > 0f) {
				EnemyDirector.instance.SetInvestigate(gameObject.transform.position, hypersonic ? investigateRadius*10 :investigateRadius);
				var shatterField = Physics.OverlapSphere(gameObject.transform.position, hypersonic ? investigateRadius*10 :investigateRadius, LayerMask.GetMask("PhysGrabObject"),  QueryTriggerInteraction.Ignore);
				// print($"Shatter Field: {shatterField.Length}");
				foreach (var field in shatterField) {
					if (!field.attachedRigidbody || field.isTrigger) continue;
					// print($"rb: {field.name}");
					var erb = field.attachedRigidbody.GetComponent<EnemyRigidbody>();
					if (!erb) continue;
					// print($"Layer: {field.gameObject.layer}");
					var controller = erb.enemy.GetComponent<EnemyGnome>();
					// print($"Controller: {controller}");
					if (controller)
						controller.OnDeath();
				}
			}
		}
		StartCoroutine(BulletDestroy());
	}

	private IEnumerator BulletDestroy() {
		yield return new WaitForSeconds(0.2f);
		while (particleSparks.isPlaying || particleSmoke.isPlaying || particleImpact.isPlaying || hitLight.enabled || shootLine.enabled || (hasHurtCollider && (bool)sonicCollider && sonicCollider.gameObject.activeSelf) || (!hasHurtCollider && (bool)hitGameObject && hitGameObject.activeSelf)) {
			yield return null;
		}
		Object.Destroy(base.gameObject);
	}

	private void LineRendererLogic() {
		if (shootLineActive) {
			shootLine.widthMultiplier = shootLineWidthCurve.Evaluate(shootLineLerp);
			shootLineLerp += Time.deltaTime * 5f;
			if (shootLineLerp >= 1f) {
				shootLine.enabled = false;
				shootLine.gameObject.SetActive(value: false);
				shootLineActive = false;
			}
		}
	}

	private void Update() {
		LineRendererLogic();
		if (!bulletHit) {
			return;
		}
		if (hasHurtCollider) {
			if (hurtColliderTimer > 0f) {
				hurtColliderTimer -= Time.deltaTime;
				if ((bool)sonicCollider) {
					sonicCollider.gameObject.SetActive(value: true);
				}
			}
			else if ((bool)sonicCollider) {
				sonicCollider.gameObject.SetActive(value: false);
			}
		}
		else
		{
			hitGameObjectDestroyTime -= Time.deltaTime;
			if (hitGameObjectDestroyTime <= 0f && (bool)hitGameObject) {
				hitGameObject.SetActive(value: false);
			}
		}
		if ((bool)hitLight) {
			hitLight.intensity = Mathf.Lerp(hitLight.intensity, 0f, Time.deltaTime * 10f);
			if (hitLight.intensity < 0.01f) {
				hitLight.enabled = false;
			}
		}
	}
}
