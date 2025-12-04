using UnityEngine;
using System.Diagnostics;
using System.Text;
using UnityEngine.Playables;

[DisallowMultipleComponent]
public class CameraChangeLogger : MonoBehaviour
{
    private Vector3 lastPos;
    private Quaternion lastRot;
    private Transform lastParent;

    void Start()
    {
        lastPos = transform.position;
        lastRot = transform.rotation;
        lastParent = transform.parent;
    }

    void Update()
    {
        if (transform.position != lastPos || transform.rotation != lastRot || transform.parent != lastParent)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"CameraChangeLogger: Camera transform changed.");
            sb.AppendLine($" New pos: {transform.position}");
            sb.AppendLine($" New rot: {transform.rotation}");
            sb.AppendLine($" Parent: {(transform.parent!=null?transform.parent.name:"(none)")}");

            // List components on the camera
            sb.AppendLine(" Components:");
            foreach (var comp in GetComponents<Component>())
            {
                if (comp == null) continue;
                sb.AppendLine($"  - {comp.GetType().Name}");
                // If it's Animator or Animation or PlayableDirector, log extra info
                var anim = comp as Animator;
                if (anim != null)
                {
                    sb.AppendLine($"     Animator.applyRootMotion={anim.applyRootMotion} enabled={anim.enabled}");
                }
                var animation = comp as Animation;
                if (animation != null)
                {
                    sb.AppendLine($"     Animation.isPlaying={animation.isPlaying}");
                }
                var pd = comp as PlayableDirector;
                if (pd != null)
                {
                    sb.AppendLine($"     PlayableDirector.state={pd.state} playOnAwake={pd.playOnAwake} enabled={pd.enabled}");
                }
            }

            // Try to find any active PlayableDirector in the scene that is playing (best-effort)
            var allPD = FindObjectsOfType<PlayableDirector>();
            foreach (var pd in allPD)
            {
                if (pd.playableAsset == null) continue;
                if (pd.state == PlayState.Playing)
                    sb.AppendLine($" Active PlayableDirector playing: {pd.gameObject.name}");
            }

            // Add managed stack trace (may be empty if change originates from native animation)
            sb.AppendLine(" Managed StackTrace:\n" + new StackTrace().ToString());

            UnityEngine.Debug.LogWarning(sb.ToString(), this);

            lastPos = transform.position;
            lastRot = transform.rotation;
            lastParent = transform.parent;
        }
    }
}
