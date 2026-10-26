using System;
using UnityEngine;

namespace Neonage.SmartTools
{
    public class SupportedReferenceTypesAttribute : PropertyAttribute
    {
        public Type[] types;
        public bool alwaysReferenceRootObject; // Will reference GameObject when dragging Component

        public SupportedReferenceTypesAttribute(params Type[] types)
        {
            this.types = types;   
        }
    }
}