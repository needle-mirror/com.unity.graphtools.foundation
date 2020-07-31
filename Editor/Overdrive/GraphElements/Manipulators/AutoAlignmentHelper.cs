using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    static class AutoAlignmentHelper
    {
        public enum AlignmentReference
        {
            Left,
            HorizontalCenter,
            Right,
            Top,
            VerticalCenter,
            Bottom
        }

        public static void SendAlignAction(GraphView graphView, AlignmentReference reference)
        {
            // Don't consider edges and invisible elements
            List<GraphElement> elements = graphView.selection.OfType<GraphElement>().Where(elem => !(elem is Edge) && elem.visible).ToList();

            // Get alignment delta for each element
            Dictionary<GraphElement, Vector2> results = GetAlignmentResults(reference, elements);

            // Dispatch Align Action
            var models = results.Keys.ToList()
                .Where(e => e is IMovableGraphElement movable && (!(e.Model is IGTFNodeModel) || movable.IsMovable))
                .Select(e => e.Model)
                .OfType<IPositioned>();
            graphView.Store.Dispatch(new AlignElementsAction(results.Values.ToList(), models.ToArray()));
        }

        static float GetAlignmentBorderPosition(AlignmentReference reference, List<GraphElement> elements)
        {
            float alignmentBorderPosition;
            if (reference == AlignmentReference.Left || reference == AlignmentReference.Top)
            {
                alignmentBorderPosition = elements.Min(elem => GetPosition(elem.GetPosition(), reference));
            }
            else if (reference == AlignmentReference.Right || reference == AlignmentReference.Bottom)
            {
                alignmentBorderPosition = elements.Max(elem => GetPosition(elem.GetPosition(), reference));
            }
            else
            {
                alignmentBorderPosition = elements.Average(elem => GetPosition(elem.GetPosition(), reference));
            }

            return alignmentBorderPosition;
        }

        static Dictionary<GraphElement, Vector2> GetAlignmentResults(AlignmentReference reference, List<GraphElement> elements)
        {
            // Get the position of the border that each selected element should align to
            float alignmentBorderPosition = GetAlignmentBorderPosition(reference, elements);

            return GetElementPositions(elements).ToDictionary(
                elementPosition => elementPosition.Key,
                elementPosition => GetDelta(reference, elementPosition.Value, alignmentBorderPosition));
        }

        static Dictionary<GraphElement, Rect> GetElementPositions(List<GraphElement> elements)
        {
            // If an element is not on a placemat, it moves according to its own position
            // If an element is on a placemat, it moves according to the overall rect's position (surrounding the placemat and the other elements on it)

            List<Placemat> allSelectedPlacemats = elements.OfType<Placemat>().ToList();

            return allSelectedPlacemats.Any() ? GetElementPositionsWithPlacemats(allSelectedPlacemats, elements) : GetElementPositionsWithoutPlacemats(elements);
        }

        static Dictionary<GraphElement, Rect> GetElementPositionsWithPlacemats(List<Placemat> allSelectedPlacemats, List<GraphElement> elements)
        {
            Dictionary<GraphElement, Rect> elementPositions = new Dictionary<GraphElement, Rect>();

            List<GraphElement> elementsOnOverallRect = new List<GraphElement>();
            List<Placemat> placematsOnOverallRect = new List<Placemat>();

            foreach (var currentPlacemat in allSelectedPlacemats.Where(currentPlacemat => !elementPositions.ContainsKey(currentPlacemat)))
            {
                elementsOnOverallRect.Add(currentPlacemat);
                placematsOnOverallRect.Add(currentPlacemat);

                Rect overallRect = currentPlacemat.GetPosition();

                foreach (Placemat placemat in allSelectedPlacemats.Where(placemat => !placemat.Equals(currentPlacemat) && placemat.layout.Overlaps(overallRect) && !elementPositions.ContainsKey(placemat)))
                {
                    // Adjust the overall rect with other placemats that are superposed with the current overall rect
                    AdjustOverallRect(ref overallRect, placemat.GetPosition());
                    placematsOnOverallRect.Add(placemat);
                    elementsOnOverallRect.Add(placemat);
                }

                foreach (GraphElement element in elements.Where(element => !(element is Placemat) && !elementPositions.ContainsKey(element)))
                {
                    if (!IsOnAPlacemat(element, allSelectedPlacemats))
                    {
                        // Element is not on a placemat, we keep its position
                        elementPositions.Add(element, element.GetPosition());
                    }
                    else if (IsOnAPlacemat(element, placematsOnOverallRect))
                    {
                        // Adjust the overall rect with elements that are superposed with the current overall rect
                        AdjustOverallRect(ref overallRect, element.GetPosition());
                        elementsOnOverallRect.Add(element);
                    }
                }

                // Assign the overall rect position to the elements that are positioned on it
                AddPositionRectToElements(ref elementPositions, elementsOnOverallRect, overallRect);

                elementsOnOverallRect.Clear();
                placematsOnOverallRect.Clear();
            }

            return elementPositions;
        }

        static Dictionary<GraphElement, Rect> GetElementPositionsWithoutPlacemats(List<GraphElement> elements)
        {
            Dictionary<GraphElement, Rect> elementPositions = new Dictionary<GraphElement, Rect>();
            AddPositionRectToElements(ref elementPositions, elements);

            return elementPositions;
        }

        static void AddPositionRectToElements(ref Dictionary<GraphElement, Rect> elementPositions, List<GraphElement> elements, Rect positionRect = default)
        {
            foreach (GraphElement element in elements)
            {
                if (!elementPositions.ContainsKey(element))
                {
                    elementPositions.Add(element, positionRect == default ? element.GetPosition() : positionRect);
                }
            }
        }

        static void AdjustOverallRect(ref Rect overallRect, Rect otherRect)
        {
            if (otherRect.yMin < overallRect.yMin)
            {
                overallRect.yMin = otherRect.yMin;
            }
            if (otherRect.xMin < overallRect.xMin)
            {
                overallRect.xMin = otherRect.xMin;
            }
            if (otherRect.yMax > overallRect.yMax)
            {
                overallRect.yMax = otherRect.yMax;
            }
            if (otherRect.xMax > overallRect.xMax)
            {
                overallRect.xMax = otherRect.xMax;
            }
        }

        static bool IsOnAPlacemat(GraphElement element, List<Placemat> placemats)
        {
            return placemats.Any(placemat => !element.Equals(placemat) && element.layout.Overlaps(placemat.layout));
        }

        static Vector2 GetDelta(AlignmentReference reference, Rect elementPosition, float alignmentBorderPos)
        {
            float offset = Math.Abs(GetPosition(elementPosition, reference) - alignmentBorderPos);
            Vector2 delta;
            switch (reference)
            {
                case AlignmentReference.Left:
                    delta = new Vector2(-offset, 0f);
                    break;
                case AlignmentReference.HorizontalCenter:
                    delta = new Vector2(elementPosition.center.x < alignmentBorderPos ? offset : -offset, 0f);
                    break;
                case AlignmentReference.Right:
                    delta = new Vector2(offset, 0f);
                    break;
                case AlignmentReference.Top:
                    delta = new Vector2(0f, -offset);
                    break;
                case AlignmentReference.VerticalCenter:
                    delta = new Vector2(0f, elementPosition.center.y < alignmentBorderPos ? offset : -offset);
                    break;
                case AlignmentReference.Bottom:
                    delta = new Vector2(0f, offset);
                    break;
                default:
                    return Vector2.zero;
            }

            return delta;
        }

        static float GetPosition(Rect rect, AlignmentReference reference)
        {
            switch (reference)
            {
                case AlignmentReference.Left:
                    return rect.x;
                case AlignmentReference.HorizontalCenter:
                    return rect.center.x;
                case AlignmentReference.Right:
                    return rect.xMax;
                case AlignmentReference.Top:
                    return rect.y;
                case AlignmentReference.VerticalCenter:
                    return rect.center.y;
                case AlignmentReference.Bottom:
                    return rect.yMax;
                default:
                    return 0;
            }
        }
    }
}
