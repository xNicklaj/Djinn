using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace TinyGiantStudio.BetterInspector
{
#if BETTEREDITOR_USE_PROJECTSETTINGS
    [FilePath("ProjectSettings/Tiny Giant Studio/Better Editor/BetterTransform Settings.asset", FilePathAttribute.Location.ProjectFolder)]
#else
    [FilePath("UserSettings/Tiny Giant Studio/Better Editor/BetterTransform Settings.asset", FilePathAttribute.Location.ProjectFolder)]
#endif
    public class BetterTransformSettings : ScriptableSingleton<BetterTransformSettings>
    {
        [FormerlySerializedAs("_currentWorkSpace")] [SerializeField]
        WorkSpace currentWorkSpace = WorkSpace.Local;

        public WorkSpace CurrentWorkSpace
        {
            get => currentWorkSpace;
            set
            {
                currentWorkSpace = value;
                Save(true);
            }
        }

        [FormerlySerializedAs("_showCopyPasteButtons")] [SerializeField]
        bool showCopyPasteButtons = true;

        public bool ShowCopyPasteButtons
        {
            get => showCopyPasteButtons;
            set
            {
                showCopyPasteButtons = value;
                Save(true);
            }
        }

        [FormerlySerializedAs("_fieldRoundingAmount")] [SerializeField]
        int fieldRoundingAmount = 5;

        public int FieldRoundingAmount
        {
            get => fieldRoundingAmount;
            set
            {
                fieldRoundingAmount = value;
                Save(true);
            }
        }

        public bool roundPositionField;
        public bool roundRotationField;
        public bool roundScaleField;

        public bool animatedFoldout = true;

        public bool pingSelfButton;

        public bool showSiblingIndex;

        [FormerlySerializedAs("showAssetGUID")]
        public bool showAssetGuid;

        public bool showWhySizeIsHiddenLabel = true;

        [FormerlySerializedAs("_loadDefaultInspector")] [SerializeField]
        bool loadDefaultInspector = true;

        public bool LoadDefaultInspector
        {
            get => loadDefaultInspector;
            set
            {
                loadDefaultInspector = value;
                Save(true);
            }
        }

        public bool logPerformance;
        public bool logDetailedPerformance = true;


        public void Reset()
        {
            loadDefaultInspector = true;

            overrideFoldoutColor = false;
            foldoutColor = new(0, 1, 0, 0.025f);
            overrideInspectorColor = true;
            inspectorColorInLocalSpace = new(1, 1, 1, 0);
            inspectorColorInWorldSpace = new(0, 0, 1, 0.025f);

            currentWorkSpace = WorkSpace.Local;
            showCopyPasteButtons = true;

            fieldRoundingAmount = 5;
            //_lockScaleAspectRatio = false;

            maxChildInspectors = 10;
            includeChildBounds = true;
            maxChildCountForSizeCalculation = 50;
            currentSizeType = SizeType.Renderer;
            lockSizeAspectRatio = false;

            showSizeGizmo = true;
            sizeGizmoOutlineThickness = 1f;
            sizeGizmoOutlineColorX = Color.red;
            sizeGizmoOutlineColorY = Color.green;
            sizeGizmoOutlineColorZ = Color.blue;
            showSizeLabelGizmo = true;
            sizeGizmoLabelSize = 10;
            sizeGizmoLabelColorX = Color.white;
            sizeGizmoLabelColorY = Color.white;
            sizeGizmoLabelColorZ = Color.white;
            sizeGizmoLabelBackgroundColorX = new(0.65f, 0, 0);
            sizeGizmoLabelBackgroundColorY = new(0.1008f, 0.4842f, 0);
            sizeGizmoLabelBackgroundColorZ = new(0, 0.1890f, 0.5220f);

            showSizeGizmoLabelOnBothSide = true;
            showSizeGizmosLabelHandle = false;
            minimumSizeForDoubleSidedLabel = 10;
            gizmoMaximumDecimalPoints = 4;
            constantSizeUpdate = false;

            showSiblingIndex = false;
            showAssetGuid = false;

            logPerformance = false;
            logDetailedPerformance = false;

            Save(true);
        }

        public void ResetToMinimal()
        {
            showSizeInLine = true;
            showSizeFoldout = false;
            showParentChildTransform = false;

            Reset();
        }

        public void ResetToDefault()
        {
            showSizeInLine = false;
            showSizeFoldout = true;
            showParentChildTransform = true;

            Reset();
        }

        public void Save()
        {
            Save(true);
        }

        #region Inspector Customization

        [FormerlySerializedAs("_overrideInspectorColor")] [SerializeField]
        bool overrideInspectorColor;

        public bool OverrideInspectorColor
        {
            get => overrideInspectorColor;
            set
            {
                overrideInspectorColor = value;
                Save(true);
            }
        }

        [FormerlySerializedAs("_inspectorColorInLocalSpace")] [SerializeField]
        Color inspectorColorInLocalSpace = new(1, 1, 1, 0f);

        public Color InspectorColorInLocalSpace
        {
            get => inspectorColorInLocalSpace;
            set
            {
                inspectorColorInLocalSpace = value;
                Save(true);
            }
        }

        [FormerlySerializedAs("_inspectorColorInWorldSpace")] [SerializeField]
        Color inspectorColorInWorldSpace = new(0, 0, 1, 0.025f);

        public Color InspectorColorInWorldSpace
        {
            get => inspectorColorInWorldSpace;
            set
            {
                inspectorColorInWorldSpace = value;
                Save(true);
            }
        }

        [FormerlySerializedAs("_overrideFoldoutColor")] [SerializeField]
        bool overrideFoldoutColor;

        public bool OverrideFoldoutColor
        {
            get => overrideFoldoutColor;
            set
            {
                overrideFoldoutColor = value;
                Save(true);
            }
        }

        [FormerlySerializedAs("_foldoutColor")] [SerializeField]
        Color foldoutColor = new(0, 1, 0, 0.025f);

        public Color FoldoutColor
        {
            get => foldoutColor;
            set
            {
                foldoutColor = value;
                Save(true);
            }
        }

        #endregion Inspector Customization


        #region Size

        [FormerlySerializedAs("_showSizeInLine")] [SerializeField]
        bool showSizeInLine;

        public bool ShowSizeInLine
        {
            get => showSizeInLine;
            set
            {
                showSizeInLine = value;
                Save(true);
            }
        }

        [FormerlySerializedAs("_showSizeFoldout")] [SerializeField]
        bool showSizeFoldout = true;

        public bool ShowSizeFoldout
        {
            get => showSizeFoldout;
            set
            {
                showSizeFoldout = value;
                Save(true);
            }
        }

        [FormerlySerializedAs("_showSizeCenter")] [SerializeField]
        bool showSizeCenter = true;

        public bool ShowSizeCenter
        {
            get => showSizeCenter;
            set
            {
                showSizeCenter = value;
                Save(true);
            }
        }

        [FormerlySerializedAs("_includeChildBounds")] [SerializeField]
        bool includeChildBounds = true;

        public bool IncludeChildBounds
        {
            get => includeChildBounds;
            set
            {
                includeChildBounds = value;
                Save(true);
            }
        }

        public bool ignoreParticleAndVFXInSizeCalculation;

        [FormerlySerializedAs("_currentSizeType")] [SerializeField]
        SizeType currentSizeType = SizeType.Renderer;

        public SizeType CurrentSizeType
        {
            get => currentSizeType;
            set
            {
                currentSizeType = value;
                Save(true);
            }
        }

        [FormerlySerializedAs("_lockSizeAspectRatio")] [SerializeField]
        bool lockSizeAspectRatio;

        public bool LockSizeAspectRatio
        {
            get => lockSizeAspectRatio;
            set
            {
                lockSizeAspectRatio = value;
                Save(true);
            }
        }

        [FormerlySerializedAs("_maxChildCountForSizeCalculation")] [SerializeField]
        int maxChildCountForSizeCalculation = 30;

        public int MaxChildCountForSizeCalculation
        {
            get => maxChildCountForSizeCalculation;
            set
            {
                maxChildCountForSizeCalculation = value;
                Save(true);
            }
        }

        [FormerlySerializedAs("_maxChildInspectors")] [SerializeField]
        int maxChildInspectors = 10;

        public int MaxChildInspector
        {
            get => maxChildInspectors;
            set
            {
                maxChildInspectors = value;
                Save(true);
            }
        }

        [FormerlySerializedAs("_constantSizeUpdate")] [SerializeField]
        bool constantSizeUpdate;

        public bool ConstantSizeUpdate
        {
            get => constantSizeUpdate;
            set
            {
                constantSizeUpdate = value;
                Save(true);
            }
        }

        public bool autoRefreshSize = true;
        public bool autoRefreshSizeInLocalSpaceInPlaymode = false;

        #region Gizmos

        [FormerlySerializedAs("_showSizeGizmo")] [SerializeField]
        bool showSizeGizmo = true;

        public bool ShowSizeOutlineGizmo
        {
            get => showSizeGizmo;
            set
            {
                showSizeGizmo = value;
                Save(true);
            }
        }

        /// <summary>
        ///     This is used when MatchGizmoColorToAxis is false
        /// </summary>
        [FormerlySerializedAs("_sizeGizmo_outlineThickness")] [SerializeField]
        float sizeGizmoOutlineThickness = 1;

        public float SizeGizmoOutlineThickness
        {
            get => sizeGizmoOutlineThickness;
            set
            {
                sizeGizmoOutlineThickness = value;
                Save(true);
            }
        }

        /// <summary>
        ///     This is used when MatchGizmoColorToAxis is false
        /// </summary>
        [FormerlySerializedAs("_sizeGizmo_outlineColor_X")] [SerializeField]
        Color sizeGizmoOutlineColorX = new(1, 0, 0, 0.75f);

        public Color SizeGizmoOutlineColorX
        {
            get => sizeGizmoOutlineColorX;
            set
            {
                sizeGizmoOutlineColorX = value;
                Save(true);
            }
        }

        /// <summary>
        ///     This is used when MatchGizmoColorToAxis is false
        /// </summary>
        [FormerlySerializedAs("_sizeGizmo_outlineColor_Y")] [SerializeField]
        Color sizeGizmoOutlineColorY = new(0, 1, 0, 0.75f);

        public Color SizeGizmoOutlineColorY
        {
            get => sizeGizmoOutlineColorY;
            set
            {
                sizeGizmoOutlineColorY = value;
                Save(true);
            }
        }

        /// <summary>
        ///     This is used when MatchGizmoColorToAxis is false
        /// </summary>
        [FormerlySerializedAs("_sizeGizmo_outlineColor_Z")] [SerializeField]
        Color sizeGizmoOutlineColorZ = new(0, 0, 1, 0.75f);

        public Color SizeGizmoOutlineColorZ
        {
            get => sizeGizmoOutlineColorZ;
            set
            {
                sizeGizmoOutlineColorZ = value;
                Save(true);
            }
        }

        [FormerlySerializedAs("_showSizeLabelGizmo")] [SerializeField]
        bool showSizeLabelGizmo = true;

        public bool ShowSizeLabelGizmo
        {
            get => showSizeLabelGizmo;
            set
            {
                showSizeLabelGizmo = value;
                Save(true);
            }
        }

        [FormerlySerializedAs("_sizeGizmoLabelSize")] [SerializeField]
        int sizeGizmoLabelSize = 10;

        public int SizeGizmoLabelSize
        {
            get => sizeGizmoLabelSize;
            set
            {
                sizeGizmoLabelSize = value;
                Save(true);
            }
        }

        [FormerlySerializedAs("_sizeGizmo_labelColor_X")] [SerializeField]
        Color sizeGizmoLabelColorX = Color.white;

        public Color SizeGizmoLabelColorX
        {
            get => sizeGizmoLabelColorX;
            set
            {
                sizeGizmoLabelColorX = value;
                Save(true);
            }
        }

        [FormerlySerializedAs("_sizeGizmo_labelColor_Y")] [SerializeField]
        Color sizeGizmoLabelColorY = Color.white;

        public Color SizeGizmoLabelColorY
        {
            get => sizeGizmoLabelColorY;
            set
            {
                sizeGizmoLabelColorY = value;
                Save(true);
            }
        }

        [FormerlySerializedAs("_sizeGizmo_labelColor_Z")] [SerializeField]
        Color sizeGizmoLabelColorZ = Color.white;

        public Color SizeGizmoLabelColorZ
        {
            get => sizeGizmoLabelColorZ;
            set
            {
                sizeGizmoLabelColorZ = value;
                Save(true);
            }
        }

        [FormerlySerializedAs("_sizeGizmo_labelBackgroundColor_X")] [SerializeField]
        Color sizeGizmoLabelBackgroundColorX = new(0.65f, 0, 0);

        public Color SizeGizmoLabelBackgroundColorX
        {
            get => sizeGizmoLabelBackgroundColorX;
            set
            {
                sizeGizmoLabelBackgroundColorX = value;
                Save(true);
            }
        }

        [FormerlySerializedAs("_sizeGizmo_labelBackgroundColor_Y")] [SerializeField]
        Color sizeGizmoLabelBackgroundColorY = new(0.1008f, 0.4842f, 0);

        public Color SizeGizmoLabelBackgroundColorY
        {
            get => sizeGizmoLabelBackgroundColorY;
            set
            {
                sizeGizmoLabelBackgroundColorY = value;
                Save(true);
            }
        }

        [FormerlySerializedAs("_sizeGizmo_labelBackgroundColor_Z")] [SerializeField]
        Color sizeGizmoLabelBackgroundColorZ = new(0, 0.1890f, 0.5220f);

        public Color SizeGizmoLabelBackgroundColorZ
        {
            get => sizeGizmoLabelBackgroundColorZ;
            set
            {
                sizeGizmoLabelBackgroundColorZ = value;
                Save(true);
            }
        }

        [FormerlySerializedAs("_showAxisOnLabel")] [SerializeField]
        bool showAxisOnLabel = true;

        public bool ShowAxisOnLabel
        {
            get => showAxisOnLabel;
            set
            {
                showAxisOnLabel = value;
                Save(true);
            }
        }

        [FormerlySerializedAs("_showUnitOnLabel")] [SerializeField]
        bool showUnitOnLabel = true;

        public bool ShowUnitOnLabel
        {
            get => showUnitOnLabel;
            set
            {
                showUnitOnLabel = value;
                Save(true);
            }
        }

        [FormerlySerializedAs("_positionLabelAtCenter")] [SerializeField]
        bool positionLabelAtCenter;

        public bool PositionLabelAtCenter
        {
            get => positionLabelAtCenter;
            set
            {
                positionLabelAtCenter = value;
                Save(true);
            }
        }

        [FormerlySerializedAs("_positionLabelAtClosestAxis")] [SerializeField]
        bool positionLabelAtClosestAxis = true;

        public bool PositionLabelAtClosestAxis
        {
            get => positionLabelAtClosestAxis;
            set
            {
                positionLabelAtClosestAxis = value;
                Save(true);
            }
        }

        [FormerlySerializedAs("_positionLabelAtCornerAxis")] [SerializeField]
        bool positionLabelAtCornerAxis;

        public bool PositionLabelAtCornerAxis
        {
            get => positionLabelAtCornerAxis;
            set
            {
                positionLabelAtCornerAxis = value;
                Save(true);
            }
        }

        [FormerlySerializedAs("_labelOffset")] [SerializeField]
        float labelOffset;

        public float LabelOffset
        {
            get => labelOffset;
            set
            {
                labelOffset = value;
                Save(true);
            }
        }

        [FormerlySerializedAs("_showSizeGizmoLabelOnBothSide")] [SerializeField]
        bool showSizeGizmoLabelOnBothSide = true;

        public bool ShowSizeGizmoLabelOnBothSide
        {
            get => showSizeGizmoLabelOnBothSide;
            set
            {
                showSizeGizmoLabelOnBothSide = value;
                Save(true);
            }
        }

        [FormerlySerializedAs("_showSizeGizmosLabelHandle")] [SerializeField]
        bool showSizeGizmosLabelHandle;

        public bool ShowSizeGizmosLabelHandle
        {
            get => showSizeGizmosLabelHandle;
            set
            {
                showSizeGizmosLabelHandle = value;
                Save(true);
            }
        }

        [FormerlySerializedAs("_minimumSizeForDoubleSidedLabel")] [SerializeField]
        int minimumSizeForDoubleSidedLabel = 10;

        public int MinimumSizeForDoubleSidedLabel
        {
            get => minimumSizeForDoubleSidedLabel;
            set
            {
                minimumSizeForDoubleSidedLabel = value;
                Save(true);
            }
        }

        [FormerlySerializedAs("_gizmoMaximumDecimalPoints")] [SerializeField]
        int gizmoMaximumDecimalPoints = 4;

        public int GizmoMaximumDecimalPoints
        {
            get => gizmoMaximumDecimalPoints;
            set
            {
                gizmoMaximumDecimalPoints = value;
                Save(true);
            }
        }

        #endregion Gizmos

        #endregion Size


        #region Parent Child Transform

        [FormerlySerializedAs("_showParentChildTransform")] [SerializeField]
        bool showParentChildTransform = true;

        public bool ShowParentChildTransform
        {
            get => showParentChildTransform;
            set
            {
                showParentChildTransform = value;
                Save(true);
            }
        }

        #endregion Parent Child Transform


        public enum SizeType
        {
            Renderer,
            Filter
        }

        [Serializable]
        public enum WorkSpace
        {
            Local,
            World,
            Both
        }
    }
}