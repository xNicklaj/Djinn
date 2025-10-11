using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using static TinyGiantStudio.BetterInspector.BetterMath;

// ReSharper disable ForCanBeConvertedToForeach

namespace TinyGiantStudio.BetterInspector
{
    //TODO Better undo strings
    /// <summary>
    /// This class handles everything that has to do with the copy, paste and rotation buttons.
    /// </summary>
    public class QuickActions
    {
        public QuickActions(Object[] targets, VisualElement root, BetterTransformEditor betterTransformEditor)
        {
            _targets = targets;
            _targetTransforms = _targets.Cast<Transform>().ToArray();

            _betterTransformEditor = betterTransformEditor;

            GroupBox normalToolbar = root.Q<GroupBox>("NormalToolbar");
            normalToolbar.schedule.Execute(() =>
            {
                normalToolbar.RegisterCallback<MouseOverEvent>(_ => { UpdatePasteButtons(); });
                WorkspaceChanged();
                UpdatePasteButtons();
            }).ExecuteLater(1000);

            SetupPositionButtons(normalToolbar);
            SetupRotationButtons(normalToolbar);
            SetupScaleButtons(normalToolbar);
            SetupLocalToolbarForBothSpace(root.Q<GroupBox>("BothSpaceToolbarForLocalSpace"));

            SetupSizeButtons(root);

            UpdatePasteButtons();
        }

        readonly Object[] _targets;
        readonly Transform[] _targetTransforms;

        readonly BetterTransformEditor _betterTransformEditor;

        Button _copyPositionButton;
        Button _copyRotationButton;
        Button _copyScaleButton;
        Button _pastePositionButton;
        Button _pasteRotationButton;
        Button _pasteScaleButton;
        Button _resetPositionButton;
        Button _resetRotationButton;
        Button _resetScaleButton;

        // When both spaces toolbars turned on, the normal toolbar is used for world space, so there is no need for another one
        Button _pasteLocalPositionBothSpaceToolbar;
        Button _pasteLocalRotationBothSpaceToolbar;
        Button _pasteLocalScaleBothSpaceToolbar;

        /// <summary>
        /// If the copied string has this, that's a copy of the hierarchy values
        /// </summary>
        const string HierarchyCopyIdentifier = "Hierarchy Copy";

        const string NoValidValueTooltip = "No valid value to paste.";

        void SetupPositionButtons(GroupBox normalToolbar)
        {
            GroupBox positionToolbar = normalToolbar.Q<GroupBox>("PositionToolbar");
            _copyPositionButton = positionToolbar.Q<Button>("Copy");
            _pastePositionButton = positionToolbar.Q<Button>("Paste");
            _resetPositionButton = positionToolbar.Q<Button>("Reset");

            _copyPositionButton.clicked += CopyPosition;
            _pastePositionButton.clicked += PastePosition;
            _resetPositionButton.clicked += ResetPosition;
        }

        void SetupRotationButtons(GroupBox normalToolbar)
        {
            GroupBox rotationToolbar = normalToolbar.Q<GroupBox>("RotationToolbar");
            _copyRotationButton = rotationToolbar.Q<Button>("Copy");
            _pasteRotationButton = rotationToolbar.Q<Button>("Paste");
            _resetRotationButton = rotationToolbar.Q<Button>("Reset");

            _copyRotationButton.clicked += CopyRotationEulerAngles;
            _pasteRotationButton.clicked += PasteRotation;
            _resetRotationButton.clicked += ResetRotation;
        }

        void SetupScaleButtons(GroupBox normalToolbar)
        {
            GroupBox rotationToolbar = normalToolbar.Q<GroupBox>("ScaleToolbar");
            _copyScaleButton = rotationToolbar.Q<Button>("Copy");
            _pasteScaleButton = rotationToolbar.Q<Button>("Paste");
            _resetScaleButton = rotationToolbar.Q<Button>("Reset");

            _copyScaleButton.clicked += CopyScale;
            _pasteScaleButton.clicked += PasteScale;
            _resetScaleButton.clicked += ResetScale;
        }

        void SetupSizeButtons(VisualElement root)
        {
            GroupBox sizeToolbar = root.Q<GroupBox>("SizeToolbar");
            Button copySizeButton = sizeToolbar.Q<Button>("Copy");
            // _pasteScaleButton = sizeToolbar.Q<Button>("Paste");
            // _resetScaleButton = sizeToolbar.Q<Button>("Reset");
            //
            copySizeButton.clicked += CopySize;
            // _pasteScaleButton.clicked += PasteScale;
            // _resetScaleButton.clicked += ResetScale;
        }

        void SetupLocalToolbarForBothSpace(VisualElement container)
        {
            GroupBox positionToolbar = container.Q<GroupBox>("PositionToolbar");
            positionToolbar.Q<Button>("Copy").clicked += CopyLocalPosition;
            _pasteLocalPositionBothSpaceToolbar =
                positionToolbar
                    .Q<
                        Button>("Paste"); //Reference to the paste button is kept to enable/disable it according to if it is possible to paste or not.
            _pasteLocalPositionBothSpaceToolbar.clicked += PasteLocalPosition;
            positionToolbar.Q<Button>("Reset").clicked += ResetLocalPosition;

            GroupBox rotationToolbar = container.Q<GroupBox>("RotationToolbar");
            rotationToolbar.Q<Button>("Copy").clicked += CopyRotationLocalEulerAngles;
            _pasteLocalRotationBothSpaceToolbar = rotationToolbar.Q<Button>("Paste");
            _pasteLocalRotationBothSpaceToolbar.clicked += PasteLocalRotation;
            rotationToolbar.Q<Button>("Reset").clicked += ResetLocalRotation;

            GroupBox scaleToolbar = container.Q<GroupBox>("ScaleToolbar");
            scaleToolbar.Q<Button>("Copy").clicked += CopyLocalScale;
            _pasteLocalScaleBothSpaceToolbar = scaleToolbar.Q<Button>("Paste");
            _pasteLocalScaleBothSpaceToolbar.clicked += PasteLocalScale;
            scaleToolbar.Q<Button>("Reset").clicked += ResetLocalScale;


            container.schedule.Execute(() =>
            {
                container.RegisterCallback<MouseOverEvent>(_ => { UpdatePasteButtons(); });
            }).ExecuteLater(4000);
        }

        public void WorkspaceChanged()
        {
            UpdatePosition_buttons();
            UpdateRotation_buttons();
            UpdateScale_buttons();
        }

        void UpdatePosition_buttons()
        {
            _copyPositionButton.tooltip = "Copy " + GetCurrentWorkspaceForToolbar() + " position.";
            _resetPositionButton.tooltip = "Reset " + GetCurrentWorkspaceForToolbar() + " position to zero.";
        }

        void UpdateRotation_buttons()
        {
            _copyRotationButton.tooltip = "Copy " + GetCurrentWorkspaceForToolbar() + " Euler Angles rotation";
            _resetRotationButton.tooltip =
                "Reset " + GetCurrentWorkspaceForToolbar() + " Euler Angles rotation to zero.";
        }

        void UpdateScale_buttons()
        {
            _copyScaleButton.tooltip = "Copy " + GetCurrentWorkspaceForToolbar() + " scale";
            if (BetterTransformSettings.instance.CurrentWorkSpace != BetterTransformSettings.WorkSpace.Local &&
                _targets.Length == 1 && _targetTransforms[0].parent != null)
                _resetScaleButton.tooltip =
                    "Reset " + GetCurrentWorkspaceForToolbar() +
                    " Scale to one. Depending on parent hierarchy, this might not give the exact value.";
            else
                _resetScaleButton.tooltip =
                    "Reset " + GetCurrentWorkspaceForToolbar() + " Scale to one.";
        }


        void UpdatePasteButtons()
        {
            bool exists;

            if (_targets.Length > 1)
            {
                GetVector3ListFromCopyBuffer(out exists, out List<string> values);
                if (exists)
                {
                    _pastePositionButton.SetEnabled(true);
                    _pasteLocalPositionBothSpaceToolbar.SetEnabled(true);
                    _pasteRotationButton.SetEnabled(true);
                    _pasteLocalRotationBothSpaceToolbar.SetEnabled(true);
                    _pasteScaleButton.SetEnabled(true);
                    _pasteLocalScaleBothSpaceToolbar.SetEnabled(true);

                    string valueString = "\n";

                    for (int i = 0; i < _targetTransforms.Length; i++)
                    {
                        if (values.Count <= i) break;
                        valueString += "\n" + _targetTransforms[i].gameObject.name + ": " + values[i] + "\n";
                    }

                    valueString += "\n";

                    UpdatePasteButtonTooltips(valueString);

                    return;
                }

                GetQuaternionListFromCopyBuffer(out exists, out values);
                if (exists)
                {
                    _pastePositionButton.SetEnabled(false);
                    _pasteLocalPositionBothSpaceToolbar.SetEnabled(false);
                    _pasteRotationButton.SetEnabled(true);
                    _pasteLocalRotationBothSpaceToolbar.SetEnabled(true);
                    _pasteScaleButton.SetEnabled(false);
                    _pasteLocalScaleBothSpaceToolbar.SetEnabled(false);

                    string valueString = "";

                    for (int i = 0; i < _targetTransforms.Length; i++)
                    {
                        if (values.Count <= i) break;
                        valueString += "\n" + _targetTransforms[i].gameObject.name + ": " + values[i] + "\n";
                    }

                    _pasteRotationButton.tooltip = "Paste " + valueString + " Quaternions to " +
                                                   GetCurrentWorkspaceForToolbar() + " rotation.";
                    _pasteLocalRotationBothSpaceToolbar.tooltip =
                        "Paste " + valueString + " Quaternions to local rotation.";
                    _pastePositionButton.tooltip = NoValidValueTooltip;
                    _pasteLocalPositionBothSpaceToolbar.tooltip = NoValidValueTooltip;
                    _pasteScaleButton.tooltip = NoValidValueTooltip;
                    _pasteLocalScaleBothSpaceToolbar.tooltip = NoValidValueTooltip;
                    return;
                }
            }

            GetVector3FromCopyBuffer(out exists, out float x, out float y, out float z);
            _pastePositionButton.SetEnabled(exists);
            _pasteLocalPositionBothSpaceToolbar.SetEnabled(exists);
            _pasteScaleButton.SetEnabled(exists);
            _pasteLocalScaleBothSpaceToolbar.SetEnabled(exists);

            if (exists)
            {
                _pasteRotationButton.SetEnabled(true);
                _pasteLocalRotationBothSpaceToolbar.SetEnabled(true);
                UpdatePasteButtonTooltips(x + "," + y + "," + z);
                return;
            }


            GetQuaternionFromCopyBuffer(out exists, out x, out y, out z, out float w);
            _pasteRotationButton.SetEnabled(exists);
            _pasteLocalRotationBothSpaceToolbar.SetEnabled(exists);
            if (exists)
            {
                string rotTooltip = "Paste Quaternion(" + x + "," + y + "," + z + "," + w + ")";
                _pasteRotationButton.tooltip = rotTooltip;
                _pasteLocalRotationBothSpaceToolbar.tooltip = rotTooltip;
            }
            else
            {
                _pasteRotationButton.tooltip = NoValidValueTooltip;
                _pasteLocalRotationBothSpaceToolbar.tooltip = NoValidValueTooltip;
            }

            _pastePositionButton.tooltip = NoValidValueTooltip;
            _pasteLocalPositionBothSpaceToolbar.tooltip = NoValidValueTooltip;
            _pasteScaleButton.tooltip = NoValidValueTooltip;
            _pasteLocalScaleBothSpaceToolbar.tooltip = NoValidValueTooltip;
        }

        void UpdatePasteButtonTooltips(string valueString)
        {
            string space = GetCurrentWorkspaceForToolbar();
            _pastePositionButton.tooltip = "Paste " + valueString + " to " + space + " Position.";
            _pasteLocalPositionBothSpaceToolbar.tooltip = "Paste " + valueString + " to Local Position.";
            _pasteRotationButton.tooltip = "Paste " + valueString + " to " + space + " Rotation Euler Angles.";
            _pasteLocalRotationBothSpaceToolbar.tooltip = "Paste " + valueString + " to Local Rotation.";
            _pasteLocalScaleBothSpaceToolbar.tooltip = "Paste " + valueString + " to Local Scale.";
            _pasteScaleButton.tooltip = "Paste " + valueString + " to " + space + " Scale.";
        }

        #region Position

        public void CopyPosition()
        {
            if (BetterTransformSettings.instance.CurrentWorkSpace == BetterTransformSettings.WorkSpace.Local)
                CopyLocalPosition();
            else
                CopyWorldPosition();
        }

        void CopyLocalPosition()
        {
            if (_targetTransforms.Length == 1)
                EditorGUIUtility.systemCopyBuffer =
                    "Vector3" + Vector3ToCopyableString(_targetTransforms[0].localPosition);
            else CopyPosition_Multitarget(false);

            UpdatePasteButtons();
        }

        void CopyWorldPosition()
        {
            if (_targetTransforms.Length == 1)
                EditorGUIUtility.systemCopyBuffer =
                    "Vector3" + Vector3ToCopyableString(_targetTransforms[0].position);
            else
                CopyPosition_Multitarget(true);

            UpdatePasteButtons();
        }

        void CopyPosition_Multitarget(bool worldSpace)
        {
            string copyString = HierarchyCopyIdentifier;

            foreach (Transform t in _targetTransforms)
            {
                copyString += "\n";
                if (worldSpace)
                    copyString += "Vector3" + Vector3ToCopyableString(t.position);
                else
                    copyString += "Vector3" + Vector3ToCopyableString(t.localPosition);
            }

            EditorGUIUtility.systemCopyBuffer = copyString;
        }

        public void PastePosition()
        {
            if (BetterTransformSettings.instance.CurrentWorkSpace == BetterTransformSettings.WorkSpace.Local)
                PasteLocalPosition();
            else
                PasteWorldPosition();
        }

        void PasteLocalPosition()
        {
            if (_targetTransforms.Length > 1)
            {
                GetVector3ListFromCopyBuffer(out bool multiTargetCopyVector3Available, out List<string> values);
                if (multiTargetCopyVector3Available)
                {
                    Undo.RecordObjects(_targets, "Position Paste.");
                    for (int i = 0; i < _targetTransforms.Length; i++)
                    {
                        if (values.Count <= i)
                            break;

                        Vector3 value = GetVector3FromString(values[i], out bool exists2);
                        if (!exists2) continue;

                        _targetTransforms[i].localPosition = value;
                        EditorUtility.SetDirty(_targetTransforms[i]);
                    }

                    return;
                }
            }

            GetVector3FromCopyBuffer(out bool exists, out float x, out float y, out float z);
            if (!exists)
                return;

            Vector3 pasteValue = new(x, y, z);
            Undo.RecordObjects(_targets, "Position Paste.");
            for (int i = 0; i < _targetTransforms.Length; i++)
            {
                _targetTransforms[i].localPosition = pasteValue;
                EditorUtility.SetDirty(_targetTransforms[i]);
            }
        }

        void PasteWorldPosition()
        {
            if (_targetTransforms.Length > 1)
            {
                GetVector3ListFromCopyBuffer(out bool multiTargetCopyVector3Available, out List<string> values);
                if (multiTargetCopyVector3Available)
                {
                    Undo.RecordObjects(_targets, "Position Paste.");
                    for (int i = 0; i < _targetTransforms.Length; i++)
                    {
                        if (values.Count <= i)
                            break;

                        Vector3 value = GetVector3FromString(values[i], out bool exists2);
                        if (!exists2) continue;

                        _targetTransforms[i].position = value;
                        EditorUtility.SetDirty(_targetTransforms[i]);
                    }

                    return;
                }
            }

            GetVector3FromCopyBuffer(out bool exists, out float x, out float y, out float z);
            if (!exists)
                return;

            Vector3 pasteValue = new(x, y, z);
            Undo.RecordObjects(_targets, "Position Paste.");
            for (int i = 0; i < _targetTransforms.Length; i++)
            {
                _targetTransforms[i].position = pasteValue;
                EditorUtility.SetDirty(_targetTransforms[i]);
            }
        }

        /// <summary>
        /// TODO UpdateWorldPositionField(); used to be called after resetting position. Fix any issues caused by it's removal 
        /// </summary>
        public void ResetPosition()
        {
            if (BetterTransformSettings.instance.CurrentWorkSpace == BetterTransformSettings.WorkSpace.Local)
                ResetLocalPosition();
            else
                ResetWorldPosition();
        }

        void ResetLocalPosition()
        {
            Undo.RecordObjects(_targets, "Reset Local Position.");
            foreach (Transform target in _targetTransforms)
            {
                target.localPosition = Vector3.zero;
                EditorUtility.SetDirty(target);
            }
        }

        void ResetWorldPosition()
        {
            Undo.RecordObjects(_targets, "Reset World Position.");
            foreach (Transform target in _targetTransforms)
            {
                target.position = Vector3.zero;
                EditorUtility.SetDirty(target);
            }
        }

        #endregion

        #region Rotation

        #region Euler Angles

        public void CopyRotationEulerAngles()
        {
            if (BetterTransformSettings.instance.CurrentWorkSpace == BetterTransformSettings.WorkSpace.Local)
                CopyRotationLocalEulerAngles();
            else CopyRotationWorldEulerAngles();
        }

        void CopyRotationLocalEulerAngles()
        {
            if (_targetTransforms.Length == 1)
            {
                EditorGUIUtility.systemCopyBuffer =
                    "Vector3" + Vector3ToCopyableString(_targetTransforms[0].localEulerAngles);
            }
            else
                CopyLocalEulerAngles_Multitarget(false);

            UpdatePasteButtons();
        }

        void CopyRotationWorldEulerAngles()
        {
            if (_targetTransforms.Length == 1)
            {
                EditorGUIUtility.systemCopyBuffer =
                    "Vector3" + Vector3ToCopyableString(_targetTransforms[0].eulerAngles);
            }
            else
                CopyLocalEulerAngles_Multitarget(true);

            UpdatePasteButtons();
        }

        void CopyLocalEulerAngles_Multitarget(bool worldSpace)
        {
            string copyString = HierarchyCopyIdentifier;

            foreach (Transform t in _targetTransforms)
            {
                copyString += "\n";

                if (worldSpace)
                    copyString += "Vector3" + Vector3ToCopyableString(t.eulerAngles);
                else
                    copyString += "Vector3" + Vector3ToCopyableString(t.localEulerAngles);
            }

            EditorGUIUtility.systemCopyBuffer = copyString;
        }

        #endregion

        #region Quaternion

        public void CopyRotationQuaternion()
        {
            if (BetterTransformSettings.instance.CurrentWorkSpace == BetterTransformSettings.WorkSpace.Local)
                CopyRotationLocalQuaternion();
            else CopyRotationWorldQuaternion();
        }

        void CopyRotationLocalQuaternion()
        {
            if (_targetTransforms.Length == 1)
            {
                EditorGUIUtility.systemCopyBuffer =
                    "Quaternion" + _targetTransforms[0].localRotation;
            }
            else
                CopyRotationQuaternion_Multitarget(false);

            UpdatePasteButtons();
        }

        void CopyRotationWorldQuaternion()
        {
            if (_targetTransforms.Length == 1)
            {
                EditorGUIUtility.systemCopyBuffer =
                    "Quaternion" + _targetTransforms[0].rotation;
            }
            else
                CopyRotationQuaternion_Multitarget(true);

            UpdatePasteButtons();
        }

        void CopyRotationQuaternion_Multitarget(bool worldSpace)
        {
            string copyString = HierarchyCopyIdentifier;

            foreach (Transform t in _targetTransforms)
            {
                copyString += "\n";

                if (worldSpace)
                    copyString += "Quaternion" + t.rotation;
                else
                    copyString += "Quaternion" + t.localRotation;
            }

            EditorGUIUtility.systemCopyBuffer = copyString;
        }

        #endregion

        public void PasteRotation()
        {
            if (BetterTransformSettings.instance.CurrentWorkSpace == BetterTransformSettings.WorkSpace.Local)
                PasteLocalRotation();
            else PasteGlobalRotation();
        }


        void PasteLocalRotation()
        {
            if (_targetTransforms.Length > 1)
            {
                GetVector3ListFromCopyBuffer(out bool multiTargetCopyVector3Available, out List<string> values);
                if (multiTargetCopyVector3Available)
                {
                    Undo.RecordObjects(_targets, "Rotation Paste.");
                    for (int i = 0; i < _targetTransforms.Length; i++)
                    {
                        if (values.Count <= i)
                            break;

                        Vector3 value = GetVector3FromString(values[i], out bool exists2);
                        if (!exists2) continue;

                        _targetTransforms[i].localRotation = Quaternion.Euler(value.x, value.y, value.z);
                        EditorUtility.SetDirty(_targetTransforms[i]);
                    }
                }
            }

            GetVector3FromCopyBuffer(out bool vector3ValueToPasteExists, out float x, out float y, out float z);
            if (vector3ValueToPasteExists)
            {
                Undo.RecordObjects(_targets, "Rotation Paste.");
                Quaternion targetAngle = Quaternion.Euler(x, y, z);

                for (int i = 0; i < _targetTransforms.Length; i++)
                {
                    _targetTransforms[i].localRotation = targetAngle;
                    EditorUtility.SetDirty(_targetTransforms[i]);
                }
            }

            GetQuaternionFromCopyBuffer(out bool quaternionExists, out float qx, out float qy, out float qz,
                out float qw);
            if (!quaternionExists) return;

            Undo.RecordObjects(_targets, "Rotation Paste.");
            Quaternion targetQuaternion = new(qx, qy, qz, qw);

            for (int i = 0; i < _targetTransforms.Length; i++)
            {
                _targetTransforms[i].localRotation = targetQuaternion;
                EditorUtility.SetDirty(_targetTransforms[i]);
            }
        }

        void PasteGlobalRotation()
        {
            if (_targetTransforms.Length > 1)
            {
                GetVector3ListFromCopyBuffer(out bool multiTargetCopyVector3Available, out List<string> values);
                if (multiTargetCopyVector3Available)
                {
                    Undo.RecordObjects(_targets, "Rotation Paste.");
                    for (int i = 0; i < _targetTransforms.Length; i++)
                    {
                        if (values.Count <= i)
                            break;

                        Vector3 value = GetVector3FromString(values[i], out bool exists2);
                        if (!exists2) continue;

                        _targetTransforms[i].rotation = Quaternion.Euler(value.x, value.y, value.z);
                        EditorUtility.SetDirty(_targetTransforms[i]);
                    }
                }
            }

            GetVector3FromCopyBuffer(out bool exists, out float x, out float y, out float z);
            if (exists)
            {
                Undo.RecordObjects(_targets, "Rotation Paste.");
                Quaternion targetAngle = Quaternion.Euler(x, y, z);

                for (int i = 0; i < _targetTransforms.Length; i++)
                {
                    _targetTransforms[i].rotation = targetAngle;
                    EditorUtility.SetDirty(_targetTransforms[i]);
                }
            }

            GetQuaternionFromCopyBuffer(out bool quaternionExists, out float qx, out float qy, out float qz,
                out float qw);
            if (!quaternionExists) return;
            Undo.RecordObjects(_targets, "Rotation Paste.");
            Quaternion targetQuaternion = new(qx, qy, qz, qw);

            for (int i = 0; i < _targetTransforms.Length; i++)
            {
                _targetTransforms[i].rotation = targetQuaternion;
                EditorUtility.SetDirty(_targetTransforms[i]);
            }
        }

        public void ResetRotation()
        {
            if (BetterTransformSettings.instance.CurrentWorkSpace == BetterTransformSettings.WorkSpace.Local)
                ResetLocalRotation();
            else
                ResetWorldRotation();
        }

        void ResetLocalRotation()
        {
            Undo.RecordObjects(_targets, "Reset rotation.");
            foreach (Transform target in _targetTransforms)
            {
                target.localRotation = Quaternion.Euler(Vector3.zero);
                EditorUtility.SetDirty(target);
            }
        }

        void ResetWorldRotation()
        {
            Undo.RecordObjects(_targets, "Reset rotation.");
            foreach (Transform target in _targetTransforms)
            {
                target.rotation = Quaternion.Euler(Vector3.zero);
                EditorUtility.SetDirty(target);
            }
        }

        #endregion

        #region Scale

        public void CopyScale()
        {
            if (BetterTransformSettings.instance.CurrentWorkSpace == BetterTransformSettings.WorkSpace.Local)
                CopyLocalScale();
            else
                CopyWorldScale();
        }

        void CopyLocalScale()
        {
            if (_targetTransforms.Length == 1)
                EditorGUIUtility.systemCopyBuffer =
                    "Vector3" + Vector3ToCopyableString(_targetTransforms[0].localScale);
            else
                CopyMultipleSelectToBuffer_scale(false);

            UpdatePasteButtons();
        }

        void CopyWorldScale()
        {
            if (_targetTransforms.Length == 1)
                EditorGUIUtility.systemCopyBuffer =
                    "Vector3" + Vector3ToCopyableString(_targetTransforms[0].lossyScale);
            else
                CopyMultipleSelectToBuffer_scale(true);

            UpdatePasteButtons();
        }

        void CopyMultipleSelectToBuffer_scale(bool worldSpace)
        {
            string copyString = HierarchyCopyIdentifier;

            if (worldSpace)
            {
                foreach (Transform t in _targetTransforms)
                {
                    copyString += "\n";
                    copyString += "Vector3" + t.lossyScale;
                }
            }
            else
            {
                foreach (Transform t in _targetTransforms)
                {
                    copyString += "\n";
                    copyString += "Vector3" + t.localScale;
                }
            }

            EditorGUIUtility.systemCopyBuffer = copyString;
        }

        public void PasteScale()
        {
            if (BetterTransformSettings.instance.CurrentWorkSpace == BetterTransformSettings.WorkSpace.Local)
                PasteLocalScale();
            else
                PasteWorldScale();
        }

        void PasteLocalScale()
        {
            if (_targetTransforms.Length > 1)
            {
                GetVector3ListFromCopyBuffer(out bool multiTargetCopyVector3Available, out List<string> values);
                if (multiTargetCopyVector3Available)
                {
                    Undo.RecordObjects(_targets, "Scale Paste.");
                    for (int i = 0; i < _targetTransforms.Length; i++)
                    {
                        if (values.Count <= i)
                            break;

                        Vector3 value = GetVector3FromString(values[i], out bool exists2);
                        if (!exists2) continue;

                        _targetTransforms[i].localScale = value;
                        EditorUtility.SetDirty(_targetTransforms[i]);
                    }

                    return;
                }
            }

            GetVector3FromCopyBuffer(out bool exists, out float x, out float y, out float z);
            if (!exists)
                return;

            Vector3 pasteValue = new(x, y, z);
            Undo.RecordObjects(_targets, "Scale Paste.");
            for (int i = 0; i < _targetTransforms.Length; i++)
            {
                _targetTransforms[i].localScale = pasteValue;
                EditorUtility.SetDirty(_targetTransforms[i]);
            }
        }

        void PasteWorldScale()
        {
            if (_targetTransforms.Length > 1)
            {
                GetVector3ListFromCopyBuffer(out bool multiTargetCopyVector3Available, out List<string> values);
                if (multiTargetCopyVector3Available)
                {
                    Undo.RecordObjects(_targets, "Position Scale.");
                    for (int i = 0; i < _targetTransforms.Length; i++)
                    {
                        if (values.Count <= i)
                            break;

                        Vector3 value = GetVector3FromString(values[i], out bool exists2);
                        if (!exists2) continue;

                        _betterTransformEditor.SetWorldScale(_targetTransforms[i], value, true);
                        EditorUtility.SetDirty(_targetTransforms[i]);
                    }

                    return;
                }
            }

            GetVector3FromCopyBuffer(out bool exists, out float x, out float y, out float z);
            if (!exists)
                return;

            Vector3 pasteValue = new(x, y, z);
            Undo.RecordObjects(_targets, "Position Paste.");
            for (int i = 0; i < _targetTransforms.Length; i++)
            {
                _betterTransformEditor.SetWorldScale(_targetTransforms[i], pasteValue, true);
                EditorUtility.SetDirty(_targetTransforms[i]);
            }
        }


        public void ResetScale()
        {
            if (BetterTransformSettings.instance.CurrentWorkSpace == BetterTransformSettings.WorkSpace.Local)
                ResetLocalScale();
            else
                ResetWorldScale();
        }

        void ResetLocalScale()
        {
            Undo.RecordObjects(_targets, "Reset position.");
            foreach (Transform target in _targetTransforms)
            {
                target.localScale = Vector3.one;
                EditorUtility.SetDirty(target);
            }
        }

        void ResetWorldScale()
        {
            Undo.RecordObjects(_targets, "Reset World Scale.");
            foreach (Transform target in _targetTransforms)
            {
                _betterTransformEditor.SetWorldScale(target, Vector3.one, true);
                EditorUtility.SetDirty(target);
            }
        }

        #endregion

        #region Size

        // ReSharper disable once MemberCanBePrivate.Global
        public void CopySize()
        {
            EditorGUIUtility.systemCopyBuffer =
                "Vector3" + Vector3ToCopyableString(_betterTransformEditor.currentBound.size *
                                                    ScalesManager.instance.CurrentUnitValue());
        }

        #endregion

        #region Utility

        public bool HasVector3ValueToPaste()
        {
            if (_targets.Length == 1)
            {
                GetVector3FromCopyBuffer(out bool exists, out _, out _, out _);
                return exists;
            }
            else
            {
                GetVector3ListFromCopyBuffer(out bool exists, out _);
                return exists;
            }
        }

        public bool HasQuaternionValueToPaste()
        {
            if (_targets.Length == 1)
            {
                GetQuaternionFromCopyBuffer(out bool exists, out _, out _, out _, out _);
                return exists;
            }
            else
            {
                GetQuaternionListFromCopyBuffer(out bool exists, out _);
                return exists;
            }
        }

        /// <summary>
        ///     This checks if any Vector3 is currently copied to systemCopyBuffer, then returns that
        /// </summary>
        public static void GetVector3FromCopyBuffer(out bool exists, out float x, out float y, out float z)
        {
            exists = false;
            x = 0;
            y = 0;
            z = 0;

            string copyBuffer = EditorGUIUtility.systemCopyBuffer;
            if (copyBuffer == null) return;
            if (!copyBuffer.Contains("Vector3")) return;
            if (copyBuffer.Length < 10) return;

            copyBuffer = copyBuffer.Substring(8, copyBuffer.Length - 9);
            string[] valueStrings = copyBuffer.Split(',');
            if (valueStrings.Length != 3) return;
            char userDecimalSeparator =
                Convert.ToChar(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);

            string sanitizedValueStringX = valueStrings[0]
                .Replace(userDecimalSeparator == ',' ? '.' : ',', userDecimalSeparator);
            if (float.TryParse(sanitizedValueStringX, NumberStyles.Float, CultureInfo.CurrentCulture,
                    out x))
                exists = true;

            if (exists)
            {
                string sanitizedValueStringY = valueStrings[1]
                    .Replace(userDecimalSeparator == ',' ? '.' : ',', userDecimalSeparator);
                if (!float.TryParse(sanitizedValueStringY, NumberStyles.Float,
                        CultureInfo.CurrentCulture, out y))
                    exists = false;
            }

            if (!exists) return;
            string sanitizedValueStringZ = valueStrings[2]
                .Replace(userDecimalSeparator == ',' ? '.' : ',', userDecimalSeparator);
            if (!float.TryParse(sanitizedValueStringZ, NumberStyles.Float,
                    CultureInfo.CurrentCulture, out z))
                exists = false;
        }

        static void GetQuaternionFromCopyBuffer(out bool exists, out float x, out float y, out float z, out float w)
        {
            exists = false;
            x = 0;
            y = 0;
            z = 0;
            w = 0;

            string copyBuffer = EditorGUIUtility.systemCopyBuffer;
            if (copyBuffer == null) return;
            if (!copyBuffer.Contains("Quaternion")) return;
            if (copyBuffer.Length <= 9) return;

            copyBuffer = copyBuffer.Substring(11, copyBuffer.Length - 12);
            string[] valueStrings = copyBuffer.Split(',');
            if (valueStrings.Length != 4) return;

            if (float.TryParse(valueStrings[0], NumberStyles.Float, CultureInfo.CurrentCulture, out x))
                exists = true;

            if (exists)
                if (!float.TryParse(valueStrings[1], NumberStyles.Float, CultureInfo.CurrentCulture,
                        out y))
                    exists = false;

            if (exists)
                if (!float.TryParse(valueStrings[2], NumberStyles.Float,
                        CultureInfo.CurrentCulture, out z))
                    exists = false;

            if (!exists) return;
            if (!float.TryParse(valueStrings[3], NumberStyles.Float, CultureInfo.CurrentCulture,
                    out w))
                exists = false;
        }


        static void GetVector3ListFromCopyBuffer(out bool exists, out List<string> values)
        {
            exists = true;

            string copyBuffer = EditorGUIUtility.systemCopyBuffer;

            if (!copyBuffer.Contains(HierarchyCopyIdentifier))
            {
                exists = false;
                values = new();
                return;
            }

            copyBuffer = copyBuffer.Substring(HierarchyCopyIdentifier.Length,
                copyBuffer.Length - HierarchyCopyIdentifier.Length);

            string[] copiedItems = copyBuffer.Split('\n');
            values = copiedItems.Where(s => !string.IsNullOrEmpty(s)).ToList();
        }

        static void GetQuaternionListFromCopyBuffer(out bool exists, out List<string> values)
        {
            exists = true;
            values = new();

            string copyBuffer = EditorGUIUtility.systemCopyBuffer;

            if (!copyBuffer.Contains(HierarchyCopyIdentifier))
            {
                values = new();
                exists = false;
                return;
            }

            copyBuffer = copyBuffer.Substring(HierarchyCopyIdentifier.Length,
                copyBuffer.Length - HierarchyCopyIdentifier.Length);

            string[] copiedItems = copyBuffer.Split('\n');
            values.AddRange(copiedItems.Where(s => !string.IsNullOrEmpty(s)));
        }

        static Vector3 GetVector3FromString(string value, out bool exists)
        {
            exists = false;

            if (value == null || !value.Contains("Vector3") || value.Length <= 9) return Vector3.zero;

            // ReSharper disable once InlineOutVariableDeclaration
            float x;
            float y = 0;
            float z = 0;

            value = value.Substring(8, value.Length - 9);
            string[] valueStrings = value.Split(',');
            if (valueStrings.Length != 3) return Vector3.zero;
            if (float.TryParse(valueStrings[0], NumberStyles.Float, CultureInfo.CurrentCulture, out x))
                exists = true;

            if (exists)
                if (!float.TryParse(valueStrings[1], NumberStyles.Float, CultureInfo.CurrentCulture,
                        out y))
                    exists = false;

            if (!exists) return new(x, y, z);
            if (!float.TryParse(valueStrings[2], NumberStyles.Float, CultureInfo.CurrentCulture,
                    out z))
                exists = false;

            return new(x, y, z);
        }


        static string GetCurrentWorkspaceForToolbar() =>
            BetterTransformSettings.instance.CurrentWorkSpace == BetterTransformSettings.WorkSpace.Both
                ? "World"
                : BetterTransformSettings.instance.CurrentWorkSpace.ToString();

        #endregion
    }
}