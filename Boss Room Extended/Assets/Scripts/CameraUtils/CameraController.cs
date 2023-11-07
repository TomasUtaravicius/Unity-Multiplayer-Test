using Cinemachine;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.BossRoom.CameraUtils
{
    public class CameraController : MonoBehaviour
    {
        private CinemachineFreeLook m_MainCamera;

        private CinemachineVirtualCamera moveCamera;
        private CinemachineVirtualCamera aimCamera;
        private GameObject cameraFollowTarget;
        void Start()
        {
            AttachCamera();
        }

        private void AttachCamera()
        {

            moveCamera = GameObject.FindWithTag("MoveCamera").GetComponent<CinemachineVirtualCamera>();
            aimCamera = GameObject.FindWithTag("AimCamera").GetComponent<CinemachineVirtualCamera>();
            cameraFollowTarget = GameObject.FindWithTag("CameraFollowTarget");
            cameraFollowTarget.transform.parent = transform;
            cameraFollowTarget.transform.localPosition = Vector3.zero;
            aimCamera.enabled = false;
            //m_MainCamera = GameObject.FindObjectOfType<CinemachineFreeLook>();
            //Assert.IsNotNull(m_MainCamera, "CameraController.AttachCamera: Couldn't find gameplay freelook camera");

            /*if (m_MainCamera)
            {
                // camera body / aim
                m_MainCamera.Follow = transform;
                m_MainCamera.LookAt = transform;
                // default rotation / zoom
                m_MainCamera.m_Heading.m_Bias = 40f;
                m_MainCamera.m_YAxis.Value = 0.5f;
            }*/
        }
    }
}
