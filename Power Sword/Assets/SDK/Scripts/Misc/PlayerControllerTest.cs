﻿using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace ThunderRoad
{

    public class PlayerControllerTest : MonoBehaviour
    {
        public Transform head;
        public float moveSpeed = 4.0f;
        public float turnSpeed = 4.0f;
        public float jumpForce = 20;

        protected new Rigidbody rigidbody;
        protected new CapsuleCollider collider;

        private Vector3 moveDirection = Vector3.zero;

        void Awake()
        {
            rigidbody = GetComponent<Rigidbody>();
            collider = GetComponent<CapsuleCollider>();
            StartCoroutine(LoadXR());
        }

        private IEnumerator LoadXR()
        {
            yield return XRGeneralSettings.Instance.Manager.InitializeLoader();
            if (XRGeneralSettings.Instance.Manager.activeLoader != null)
            {
                XRGeneralSettings.Instance.Manager.StartSubsystems();
                InputDevice headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
                while (!headDevice.isValid)
                {
                    headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
                    yield return new WaitForSeconds(1);
                }
                Time.fixedDeltaTime = Time.timeScale / XRDevice.refreshRate;
            }
        }

        private void OnDisable()
        {
            if (XRGeneralSettings.Instance.Manager.activeLoader != null)
            {
                XRGeneralSettings.Instance.Manager.StopSubsystems();
            }
        }

        private void OnDestroy()
        {
            if (XRGeneralSettings.Instance.Manager.activeLoader != null)
            {
                XRGeneralSettings.Instance.Manager.DeinitializeLoader();
            }
        }

        void FixedUpdate()
        {
            collider.center = new Vector3(this.transform.InverseTransformPoint(head.position).x, 0, this.transform.InverseTransformPoint(head.position).z);

            InputDevice leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if (leftDevice.isValid)
            {
                // Move
                leftDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 axis);
                Vector3 moveDirection = Quaternion.Euler(0, head.transform.rotation.eulerAngles.y, 0) * new Vector3(axis.x, 0, axis.y);
                moveDirection *= moveSpeed;
                if (moveDirection.magnitude < 0.1f) moveDirection = Vector3.zero;
                rigidbody.velocity = new Vector3(moveDirection.x, rigidbody.velocity.y, moveDirection.z);
            }

            InputDevice rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (rightDevice.isValid)
            {
                // Turn
                rightDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 axis);
                if (axis.x > 0.1f || axis.x < -0.1f) this.transform.RotateAround(head.position, Vector3.up, axis.x * turnSpeed);

                // Jump
                rightDevice.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bool axisClick);
                rightDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool buttonClick);
                if (axisClick || buttonClick)
                {
                    rigidbody.AddForce(new Vector3(0, jumpForce, 0), ForceMode.Impulse);
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Zone"))
            {
                Zone zone = other.GetComponent<Zone>();
                if (zone.teleportPlayer)
                {
                    this.transform.position = zone.customTeleportTarget ? zone.customTeleportTarget.position : Level.current.playerStart.position;
                    this.transform.rotation = zone.customTeleportTarget ? zone.customTeleportTarget.rotation : Level.current.playerStart.rotation;
                }
                zone.playerEnterEvent.Invoke(this);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.layer == LayerMask.NameToLayer("Zone"))
            {
                Zone zone = other.GetComponent<Zone>();
                zone.playerExitEvent.Invoke(this);
            }
        }
    }
}