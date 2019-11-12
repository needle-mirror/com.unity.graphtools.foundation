using System;

namespace UnityEditor.VisualScripting.Model
{
    [Serializable]
    public class StringConstantModel : ConstantNodeModel<String>
    {
        public StringConstantModel()
        {
            value = "";
        }
    }
}
