﻿using BS_Utils.Utilities;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR;

namespace VRrhythmLeague
{
    public class OnlineVRController : VRController
    {
        public OnlinePlayerController owner;

        Vector3 targetPos;
        Quaternion targetRot;

        VRPlatformHelper _platformHelper;
        VRControllerTransformOffset _transformOffset;

        Vector3 _localPos;
        Quaternion _localRot;
        InputDevice _saberInput;

        public OnlineVRController()
        {
            VRController original = GetComponent<VRController>();

            foreach (FieldInfo info in original.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
            {
                info.SetValue(this, info.GetValue(original));
            }

            _platformHelper = original.GetPrivateField<VRPlatformHelper>("_vrPlatformHelper");
            _transformOffset = original.GetPrivateField<VRControllerTransformOffset>("_transformOffset");

            Destroy(original);
        }

        public new void Update()
        {
            if (Client.Instance == null || !Client.Instance.connected)
            {
                DefaultUpdate();
            }
            else
            {
                if (owner != null && owner.playerInfo != null)
                {
                    targetPos = (node == XRNode.LeftHand ? owner.playerInfo.updateInfo.leftHandPos : owner.playerInfo.updateInfo.rightHandPos);
                    targetRot = (node == XRNode.LeftHand ? owner.playerInfo.updateInfo.leftHandRot : owner.playerInfo.updateInfo.rightHandRot);

                    transform.position = targetPos + Vector3.right * owner.avatarOffset;
                    transform.rotation = targetRot;
                }
                else
                {
                    if(Time.frameCount % 90 == 0)
                    {
                        Plugin.log.Warn(owner == null ? "Controller owner is null!" : "Controller owner's player info is null!");
                    }
                }
            }
        }

        private void DefaultUpdate()
        {
            if (_saberInput == null)
                _saberInput = InputDevices.GetDeviceAtXRNode(node);

            _saberInput.TryGetFeatureValue(CommonUsages.devicePosition, out _localPos);
            transform.localPosition = _localPos;
            _saberInput.TryGetFeatureValue(CommonUsages.deviceRotation, out _localRot);
            transform.localRotation = _localRot;

            if(_transformOffset != null)
                _platformHelper.AdjustPlatformSpecificControllerTransform(node, transform, _transformOffset.positionOffset, _transformOffset.rotationOffset);
            else
                _platformHelper.AdjustPlatformSpecificControllerTransform(node, transform, Vector3.zero, Vector3.zero);
        }
    }
}
