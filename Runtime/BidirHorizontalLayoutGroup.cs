using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
#endif

namespace NoSuchStudio.UI {

    [AddComponentMenu("Layout/Bidirectional Horizontal Layout Group", 153)]
    /// <summary>
    ///   Layout child layout elements side by side.
    /// </summary>
    public class BidirHorizontalLayoutGroup : HorizontalOrVerticalLayoutGroup {

        static readonly Vector2 TopLeft = Vector2.up;
        static readonly Vector2 BottomLeft = Vector2.zero;
        static readonly Vector2 TopRight = Vector2.right + Vector2.up;
        static readonly Vector2 BottomRight = Vector2.right;

        [SerializeField] protected bool m_IsReverse = false;
        public bool IsReverse {
            get { return m_IsReverse; }
            set { SetProperty(ref m_IsReverse, value); }
        }

        protected BidirHorizontalLayoutGroup() { }

        /// <summary>
        /// Called by the layout system. Also see ILayoutElement
        /// </summary>
        public override void CalculateLayoutInputHorizontal() {
            base.CalculateLayoutInputHorizontal();
            CalcAlongAxis(0, false);
        }

        /// <summary>
        /// Called by the layout system. Also see ILayoutElement
        /// </summary>
        public override void CalculateLayoutInputVertical() {
            CalcAlongAxis(1, false);
        }

        /// <summary>
        /// Called by the layout system. Also see ILayoutElement
        /// </summary>
        public override void SetLayoutHorizontal() {
            // SetChildrenAlongAxis(0, false);
            SetChildrenAlongAxisWithReverse(0, false, m_IsReverse);
        }

        /// <summary>
        /// Called by the layout system. Also see ILayoutElement
        /// </summary>
        public override void SetLayoutVertical() {
            SetChildrenAlongAxisWithReverse(1, false, false);
        }

        private void GetChildSizes(RectTransform child, int axis, bool controlSize, bool childForceExpand,
                                    out float min, out float preferred, out float flexible) {
            if (!controlSize) {
                min = child.sizeDelta[axis];
                preferred = min;
                flexible = 0;
            } else {
                min = LayoutUtility.GetMinSize(child, axis);
                preferred = LayoutUtility.GetPreferredSize(child, axis);
                flexible = LayoutUtility.GetFlexibleSize(child, axis);
            }

            if (childForceExpand)
                flexible = Mathf.Max(flexible, 1);
        }

        /// <summary>
        /// Set the positions and sizes of the child layout elements for the given axis.
        /// </summary>
        /// <param name="axis">The axis to handle. 0 is horizontal and 1 is vertical.</param>
        /// <param name="isVertical">Is this group a vertical group?</param>
        /// <param name="isReverse">Reverse the order of items?</param>
        protected void SetChildrenAlongAxisWithReverse(int axis, bool isVertical, bool isReverse) {
            float size = rectTransform.rect.size[axis];
            bool controlSize = (axis == 0 ? m_ChildControlWidth : m_ChildControlHeight);
            bool useScale = (axis == 0 ? m_ChildScaleWidth : m_ChildScaleHeight);
            bool childForceExpandSize = (axis == 0 ? m_ChildForceExpandWidth : m_ChildForceExpandHeight);
            float alignmentOnAxis = GetAlignmentOnAxis(axis);

            bool alongOtherAxis = (isVertical ^ (axis == 1));
            if (alongOtherAxis) {
                float innerSize = size - (axis == 0 ? padding.horizontal : padding.vertical);
                for (int i = 0; i < rectChildren.Count; i++) {
                    RectTransform child = rectChildren[i];
                    float min, preferred, flexible;
                    GetChildSizes(child, axis, controlSize, childForceExpandSize, out min, out preferred, out flexible);
                    float scaleFactor = useScale ? child.localScale[axis] : 1f;

                    float requiredSpace = Mathf.Clamp(innerSize, min, flexible > 0 ? size : preferred);
                    float startOffset = GetStartOffsetWithReverse(axis, isReverse, requiredSpace * scaleFactor);
                    if (controlSize) {
                        SetChildAlongAxisWithScaleAndDirection(child, axis, isReverse, startOffset, requiredSpace, scaleFactor);
                    } else {
                        float offsetInCell = (requiredSpace - child.sizeDelta[axis]) * alignmentOnAxis;
                        SetChildAlongAxisWithScaleAndDirection(child, axis, isReverse, startOffset + offsetInCell, scaleFactor);
                    }
                }
            } else {
                float pos = (axis == 0 ? (m_IsReverse ? padding.right : padding.left) : (m_IsReverse ? padding.bottom : padding.top));
                float itemFlexibleMultiplier = 0;
                float surplusSpace = size - GetTotalPreferredSize(axis);

                if (surplusSpace > 0) {
                    if (GetTotalFlexibleSize(axis) == 0)
                        pos = GetStartOffsetWithReverse(axis, isReverse, GetTotalPreferredSize(axis) - (axis == 0 ? padding.horizontal : padding.vertical));
                    else if (GetTotalFlexibleSize(axis) > 0)
                        itemFlexibleMultiplier = surplusSpace / GetTotalFlexibleSize(axis);
                }

                float minMaxLerp = 0;
                if (GetTotalMinSize(axis) != GetTotalPreferredSize(axis))
                    minMaxLerp = Mathf.Clamp01((size - GetTotalMinSize(axis)) / (GetTotalPreferredSize(axis) - GetTotalMinSize(axis)));

                for (int i = 0; i < rectChildren.Count; i++) {
                    RectTransform child = rectChildren[i];
                    float min, preferred, flexible;
                    GetChildSizes(child, axis, controlSize, childForceExpandSize, out min, out preferred, out flexible);
                    float scaleFactor = useScale ? child.localScale[axis] : 1f;

                    float childSize = Mathf.Lerp(min, preferred, minMaxLerp);
                    childSize += flexible * itemFlexibleMultiplier;
                    if (controlSize) {
                        SetChildAlongAxisWithScaleAndDirection(child, axis, isReverse, pos, childSize, scaleFactor);
                    } else {
                        float offsetInCell = (childSize - child.sizeDelta[axis]) * alignmentOnAxis;
                        SetChildAlongAxisWithScaleAndDirection(child, axis, isReverse, pos + offsetInCell, scaleFactor);
                    }
                    pos += childSize * scaleFactor + spacing;
                }
            }
        }

        /// <summary>
        /// Set the position and size of a child layout element along the given axis.
        /// </summary>
        /// <param name="rect">The RectTransform of the child layout element.</param>
        /// <param name="axis">The axis to set the position and size along. 0 is horizontal and 1 is vertical.</param>
        /// <param name="pos">The position from the left side or top.</param>
        /// <param name="size">The size.</param>
        protected void SetChildAlongAxisWithScaleAndDirection(RectTransform rect, int axis, bool reverse, float pos, float size, float scaleFactor) {
            if (rect == null)
                return;

            m_Tracker.Add(this, rect,
                DrivenTransformProperties.Anchors |
                (axis == 0 ?
                    (DrivenTransformProperties.AnchoredPositionX | DrivenTransformProperties.SizeDeltaX) :
                    (DrivenTransformProperties.AnchoredPositionY | DrivenTransformProperties.SizeDeltaY)
                )
            );

            // Inlined rect.SetInsetAndSizeFromParentEdge(...) and refactored code in order to multiply desired size by scaleFactor.
            // sizeDelta must stay the same but the size used in the calculation of the position must be scaled by the scaleFactor.
            // Debug.LogFormat("here {0} size forced! a: {1}, r: {2}", name, axis, reverse);
            if (axis == 0) {
                rect.anchorMin = reverse ? TopRight : TopLeft;
                rect.anchorMax = reverse ? TopRight : TopLeft;
            }
            // Debug.LogFormat("here {0} size forced2!\nanchmin: {1}\nanchmax: {2}", name, rect.anchorMin, rect.anchorMax);

            Vector2 sizeDelta = rect.sizeDelta;
            sizeDelta[axis] = size;
            rect.sizeDelta = sizeDelta;

            Vector2 anchoredPosition = rect.anchoredPosition;
            anchoredPosition[axis] = (axis == 0 ^ reverse) ? (pos + size * rect.pivot[axis] * scaleFactor) : (-pos - size * (1f - rect.pivot[axis]) * scaleFactor);
            rect.anchoredPosition = anchoredPosition;
        }

        protected void SetChildAlongAxisWithScaleAndDirection(RectTransform rect, int axis, bool isReverse, float pos, float scaleFactor) {
            if (rect == null)
                return;

            m_Tracker.Add(this, rect,
                DrivenTransformProperties.Anchors |
                (axis == 0 ? 
                    DrivenTransformProperties.AnchoredPositionX : 
                    DrivenTransformProperties.AnchoredPositionY));

            // Inlined rect.SetInsetAndSizeFromParentEdge(...) and refactored code in order to multiply desired size by scaleFactor.
            // sizeDelta must stay the same but the size used in the calculation of the position must be scaled by the scaleFactor.
            // Debug.LogFormat("here {0} size external! a: {1}, r: {2}", name, axis, isReverse);
            if (axis == 0) {
                rect.anchorMin = isReverse ? TopRight : TopLeft;
                rect.anchorMax = isReverse ? TopRight : TopLeft;
            }
            // Debug.LogFormat("here {0} size external2!\nanchmin: {1}\nanchmax: {2}", name, rect.anchorMin, rect.anchorMax);

            Vector2 anchoredPosition = rect.anchoredPosition;
            anchoredPosition[axis] = (axis == 0 ^ isReverse) ? (pos + rect.sizeDelta[axis] * rect.pivot[axis] * scaleFactor) : (-pos - rect.sizeDelta[axis] * (1f - rect.pivot[axis]) * scaleFactor);
            rect.anchoredPosition = anchoredPosition;
        }

        /// <summary>
        /// Returns the calculated position of the first child layout element along the given axis.
        /// </summary>
        /// <param name="axis">The axis index. 0 is horizontal and 1 is vertical.</param>
        /// <param name="requiredSpaceWithoutPadding">The total space required on the given axis for all the layout elements including spacing and excluding padding.</param>
        /// <returns>The position of the first child along the given axis.</returns>
        protected float GetStartOffsetWithReverse(int axis, bool isReverse, float requiredSpaceWithoutPadding) {
            float requiredSpace = requiredSpaceWithoutPadding + (axis == 0 ? padding.horizontal : padding.vertical);
            float availableSpace = rectTransform.rect.size[axis];
            float surplusSpace = availableSpace - requiredSpace;
            float alignmentOnAxis = GetAlignmentOnAxis(axis);
            return (axis == 0 ? (isReverse ? padding.right : padding.left) : (isReverse ? padding.bottom : padding.top)) + surplusSpace * alignmentOnAxis;
        }
    }
}
