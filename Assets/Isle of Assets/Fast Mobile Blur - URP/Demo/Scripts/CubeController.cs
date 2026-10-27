using UnityEngine;

namespace FastMobileBlurURP2023.Demo
{
    [HelpURL("https://assetstore.unity.com/packages/slug/242848")]
    public class CubeController : MonoBehaviour
    {
        [SerializeField]
        private float speed = 0f, length = 0f;

        private Vector3 startRotation;

        /// <summary>
        /// Saving the initial rotation
        /// </summary>
        private void Start()
        {
            startRotation = transform.eulerAngles;
        }

        /// <summary>
        /// Rotation animation
        /// </summary>
        private void Update()
        {
            transform.eulerAngles = Vector3.Lerp(transform.eulerAngles, startRotation + Vector3.one * Mathf.PingPong(Time.time * speed, length), speed * Time.deltaTime);
        }
    }
}