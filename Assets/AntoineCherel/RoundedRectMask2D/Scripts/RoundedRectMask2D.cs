using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using TMPro;


#if UNITY_2021_1_OR_NEWER
using UnityEngine.Pool;
#endif

namespace ACRoundedRectMask
{
    /// <summary>
    /// Overrides the RectMask2D.PerformClipping method to add extra checks before doing exhaustive culling on 
    /// each maskable target.
    /// </summary>
    public class RoundedRectMask2D : RectMask2D
    {
        public static readonly string RadiiPropertyName = "_ClipRectRadii";


        [SerializeField]
        private bool independantRadii;
        [Tooltip("The four corner radii of the rounded rect. (x: top left, y: top right, z: bottom left, w: bottom right)")]
        [SerializeField]
        private Vector4 radii = Vector4.one * 10.0f;
        public Vector4 Radii
        {
            get => radii;
            set
            {
                radii = value;
                MaskUtilities.Notify2DMaskStateChanged(this);
                ForceClip = true;
            }
        }

        [Tooltip("If not set to true, you will need to handle that all masked UI elements have their own material instances")]
        [SerializeField]
        private bool cloneMaskableMaterialsOnStart = true;

        private static int clipRectRadiiID = 0;


        private HashSet<IClippable> clipTargets = null;
        private HashSet<MaskableGraphic> maskableTargets = null;
        private int lastclipTargetsCount = 0;
        private int lastmaskableTargetsCount = 0;
        private bool shouldRecalculateClipRects = false;

        private Canvas cachedCanvas = null;
        private Vector3[] cachedCorners = new Vector3[4];
        private Rect lastClipRectCanvasSpace = new Rect();
        private Vector2Int lastSoftness = new Vector2Int();
        private List<RectMask2D> clippers = new List<RectMask2D>();

        #region MonoBehaviour Implementation
        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();
            shouldRecalculateClipRects = true;
            ForceClip = true;
        }

#if UNITY_EDITOR
        /// <inheritdoc />
        protected override void OnValidate()
        {
            base.OnValidate();
            shouldRecalculateClipRects = true;
            ForceClip = true;
        }
#endif
        /// <inheritdoc />
        protected override void OnDidApplyAnimationProperties()
        {
            base.OnDidApplyAnimationProperties();

            shouldRecalculateClipRects = true;
            ForceClip = true;
        }

        #endregion MonoBehaviour Implementation

        #region RectMask2D Implementation

        /// <inheritdoc />
        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            shouldRecalculateClipRects = true;
        }

        /// <inheritdoc />
        protected override void OnCanvasHierarchyChanged()
        {
            cachedCanvas = null;
            base.OnCanvasHierarchyChanged();
            shouldRecalculateClipRects = true;
        }


        protected override void Start()
        {
            base.Start();

            shouldRecalculateClipRects = true;
            PerformClipping();

            if (cloneMaskableMaterialsOnStart && maskableTargets != null)
            {
                foreach (MaskableGraphic mg in maskableTargets)
                {
                    if (mg.materialForRendering.Equals(mg.material))
                    {
                        Material m = new Material(mg.material);
                        mg.material = m;
                    }
                    else if (mg is TMP_Text tmpText)
                    {
                        Material m = new Material(tmpText.fontMaterial);
                        tmpText.fontMaterial = m;
                    }
                    else
                    {
                        Debug.Log("[RoundedRectMask2d] Can't clone material for " + mg.name + ". This will result in same rounded corners for all assets sharing its materiel " + mg.materialForRendering);
                        continue;
                    }
                    
                    OnSetClipRect(mg);
                }
            }
        }


        /// <summary>
        /// Improves the base class method by:
        /// - Checks if the canvas renderer has moved before exhaustive culling.
        /// - Interleaves UpdateClipSoftness so objects are not iterated over twice.
        /// - Adds a OnSetClipRect callback for derived classes to use.
        /// </summary>
        public override void PerformClipping()
        {
            // Not calling the base class method intentionally to provide a more optimal version.
            //base.PerformClipping();

            if (clipRectRadiiID == 0)
            {
                clipRectRadiiID = Shader.PropertyToID(RadiiPropertyName);
            }


            Initialize();

            if (ReferenceEquals(Canvas, null))
            {
                return;
            }

            //TODO See if an IsActive() test would work well here or whether it might cause unexpected side effects (re case 776771)

            // if the parents are changed
            // or something similar we
            // do a recalculate here
            if (shouldRecalculateClipRects || ForceClip)
            {
                MaskUtilities.GetRectMasksForClip(this, clippers);
                shouldRecalculateClipRects = false;
            }

            // get the compound rects from
            // the clippers that are valid
            bool validRect = true;
            Rect clipRect = Clipping.FindCullAndClipWorldRect(clippers, out validRect);

            // If the mask is in ScreenSpaceOverlay/Camera render mode, its content is only rendered when its rect
            // overlaps that of the root canvas.
            RenderMode renderMode = Canvas.rootCanvas.renderMode;
            bool maskIsCulled =
                (renderMode == RenderMode.ScreenSpaceCamera || renderMode == RenderMode.ScreenSpaceOverlay) &&
                !clipRect.Overlaps(RootCanvasRect, true);

            if (maskIsCulled)
            {
                // Children are only displayed when inside the mask. If the mask is culled, then the children
                // inside the mask are also culled. In that situation, we pass an invalid rect to allow callees
                // to avoid some processing.
                clipRect = Rect.zero;
                validRect = false;
            }

            if (clipRect != lastClipRectCanvasSpace || softness != lastSoftness)
            {
                foreach (IClippable clipTarget in clipTargets)
                {
                    clipTarget.SetClipRect(clipRect, validRect);
                    clipTarget.SetClipSoftness(softness);
                }

                foreach (MaskableGraphic maskableTarget in maskableTargets)
                {
                    maskableTarget.SetClipRect(clipRect, validRect);
                    maskableTarget.SetClipSoftness(softness);
                    OnSetClipRect(maskableTarget);

                    maskableTarget.Cull(clipRect, validRect);
                }
            }
            else if (ForceClip)
            {
                foreach (IClippable clipTarget in clipTargets)
                {
                    clipTarget.SetClipRect(clipRect, validRect);
                    clipTarget.SetClipSoftness(softness);
                }

                foreach (MaskableGraphic maskableTarget in maskableTargets)
                {
                    maskableTarget.SetClipRect(clipRect, validRect);
                    maskableTarget.SetClipSoftness(softness);
                    OnSetClipRect(maskableTarget);

                    if (maskableTarget.canvasRenderer.hasMoved)
                    {
                        maskableTarget.Cull(clipRect, validRect);
                    }
                }
            }
            else
            {
                foreach (MaskableGraphic maskableTarget in maskableTargets)
                {
                    if (!maskableTarget.canvasRenderer.hasMoved)
                    {
                        continue;
                    }

                    maskableTarget.Cull(clipRect, validRect);
                }
            }

            ForceClip = false;
            lastClipRectCanvasSpace = clipRect;
            lastSoftness = softness;
        }

        #endregion RectMask2D Implementation

        public bool ForceClip
        {
            get
            {
                // This is an imprecise check if a clip or mask target gets added then removed on the same frame.
                // But... the alternative is we reflect into m_ForceClip base member which would be a per frame allocation due to it being a value type.
                // If this check is return false negatives in your scenario, then set ForceClip to true.
                return clipTargets.Count != lastclipTargetsCount ||
                       maskableTargets.Count != lastmaskableTargetsCount;
            }
            set
            {
                if (value == true)
                {
                    lastclipTargetsCount = 0;
                    lastmaskableTargetsCount = 0;
                }
                else
                {
                    Initialize();

                    lastclipTargetsCount = clipTargets.Count;
                    lastmaskableTargetsCount = maskableTargets.Count;
                }
            }
        }

        /// <summary>
        /// Callback whenever the clip rect is mutated.
        /// </summary>
        protected virtual void OnSetClipRect(IClippable clippable)
        { 
            
        }

        /// <summary>
        /// Callback whenever the clip rect is mutated.
        /// </summary>
        protected virtual void OnSetClipRect(MaskableGraphic maskableTarget)
        {
            Material targetMaterial = maskableTarget.materialForRendering;

            if (targetMaterial != null)
            {
                targetMaterial.SetVector(clipRectRadiiID, Radii);
            }

            //Debug.Log("Setting clip rect for " + maskableTarget.name);
        }

        private void Initialize()
        {
            // Check if we have already initialized.
            if (clipTargets != null)
            {
                return;
            }

            // Many of the properties we need access to for clipping are not exposed. So, we have to do reflection to get access to them.
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            clipTargets = (HashSet<IClippable>)typeof(RectMask2D).GetField("m_ClipTargets", bindFlags).GetValue(this);
            maskableTargets = (HashSet<MaskableGraphic>)typeof(RectMask2D).GetField("m_MaskableTargets", bindFlags).GetValue(this);
        }

        private Canvas Canvas
        {
            get
            {
                if (cachedCanvas == null)
                {
#if UNITY_2021_1_OR_NEWER
                    var list = ListPool<Canvas>.Get();
                    gameObject.GetComponentsInParent(false, list);
                    if (list.Count > 0)
                        cachedCanvas = list[list.Count - 1];
                    else
                        cachedCanvas = null;
                    ListPool<Canvas>.Release(list);
#else
                    var list = gameObject.GetComponentsInParent<Canvas>(false);
                    if (list.Length > 0)
                        cachedCanvas = list[list.Length - 1];
                    else
                        cachedCanvas = null;
#endif
                }

                return cachedCanvas;
            }
        }

        private Rect RootCanvasRect
        {
            get
            {
                rectTransform.GetWorldCorners(cachedCorners);

                if (!ReferenceEquals(Canvas, null))
                {
                    Canvas rootCanvas = Canvas.rootCanvas;
                    for (int i = 0; i < 4; ++i)
                        cachedCorners[i] = rootCanvas.transform.InverseTransformPoint(cachedCorners[i]);
                }

                return new Rect(cachedCorners[0].x, cachedCorners[0].y, cachedCorners[2].x - cachedCorners[0].x, cachedCorners[2].y - cachedCorners[0].y);
            }
        }
    }
}
