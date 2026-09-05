using UnityEngine;

public class GrapplingGun : MonoBehaviour
{
    [Header("References")]
    public Transform gunTip;
    public Transform cam;
    public Rigidbody player;
    public LineRenderer lr;

    [Header("Grapple Settings")]
    public LayerMask whatIsGrappleable;
    public float maxDistance = 25f;
    public KeyCode grappleKey = KeyCode.Mouse1;

    [Header("Spring Joint Settings")]
    public float spring = 4.5f;
    public float damper = 7f;
    public float massScale = 4.5f;

    [Header("Reel Settings")]
    [Tooltip("Units per second the rope shortens/lengthens while holding the reel keys.")]
    public float reelSpeed = 6f;
    public float minRopeLength = 2f;
    public KeyCode reelInKey = KeyCode.Q;
    public KeyCode reelOutKey = KeyCode.E;

    [Header("Rope Colors")]
    public Color idleRopeColor = Color.white;
    public Color reelInColor = Color.green;
    public Color reelOutColor = new Color(1f, 0.5f, 0f);

    [Header("Crosshair")]
    public UnityEngine.UI.Image crosshairImage;
    public Color crosshairDefaultColor = Color.white;
    public Color crosshairInRangeColor = Color.green;

    private SpringJoint joint;
    private Vector3 grapplePoint;
    private float currentMaxDistance;

    private void Update()
    {
        if (Input.GetKeyDown(grappleKey))
        {
            StartGrapple();
        }
        else if (Input.GetKeyUp(grappleKey))
        {
            StopGrapple();
        }

        if (joint != null)
        {
            bool reelingIn = Input.GetKey(reelInKey);
            bool reelingOut = !reelingIn && Input.GetKey(reelOutKey);

            if (reelingIn)
            {
                ReelIn(reelSpeed * Time.deltaTime);
            }
            if (reelingOut)
            {
                ReelOut(reelSpeed * Time.deltaTime);
            }

            UpdateRopeColor(reelingIn, reelingOut);
            DrawRope();

            if (crosshairImage != null)
            {
                crosshairImage.color = crosshairInRangeColor;
            }
        }
        else
        {
            UpdateCrosshairRangeIndicator();
        }
    }

    private void UpdateCrosshairRangeIndicator()
    {
        if (crosshairImage == null) return;
        bool inRange = Physics.Raycast(cam.position, cam.forward, maxDistance, whatIsGrappleable);
        crosshairImage.color = inRange ? crosshairInRangeColor : crosshairDefaultColor;
    }

    private void UpdateRopeColor(bool reelingIn, bool reelingOut)
    {
        Color c = reelingIn ? reelInColor : (reelingOut ? reelOutColor : idleRopeColor);
        lr.startColor = c;
        lr.endColor = c;
    }

    private void StartGrapple()
    {
        RaycastHit hit;
        if (Physics.Raycast(cam.position, cam.forward, out hit, maxDistance, whatIsGrappleable))
        {
            grapplePoint = hit.point;

            joint = player.gameObject.AddComponent<SpringJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = grapplePoint;

            currentMaxDistance = Vector3.Distance(player.position, grapplePoint);

            joint.maxDistance = currentMaxDistance;
            joint.minDistance = 0f;

            joint.spring = spring;
            joint.damper = damper;
            joint.massScale = massScale;

            lr.positionCount = 2;
        }
    }

    private void StopGrapple()
    {
        lr.positionCount = 0;
        if (joint != null)
        {
            Destroy(joint);
            joint = null; // Destroy() is deferred to end of frame; null it now so this Update doesn't call DrawRope() with an empty line
        }
    }

    // Shortens the rope, pulling the player toward the grapple point (Spider-Man-style reel-in).
    public void ReelIn(float amount)
    {
        if (joint == null) return;
        currentMaxDistance = Mathf.Clamp(currentMaxDistance - amount, minRopeLength, maxDistance);
        joint.maxDistance = currentMaxDistance;
    }

    // Lengthens the rope, letting the player swing further out or drop lower.
    public void ReelOut(float amount)
    {
        if (joint == null) return;
        currentMaxDistance = Mathf.Clamp(currentMaxDistance + amount, minRopeLength, maxDistance);
        joint.maxDistance = currentMaxDistance;
    }

    private void DrawRope()
    {
        lr.SetPosition(0, gunTip.position);
        lr.SetPosition(1, grapplePoint);
    }

    public bool IsGrappling()
    {
        return joint != null;
    }

    public Vector3 GetGrapplePoint()
    {
        return grapplePoint;
    }
}
