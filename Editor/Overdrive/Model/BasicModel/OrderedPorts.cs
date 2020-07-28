using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine.Assertions;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [PublicAPI]
    public class OrderedPorts : IReadOnlyDictionary<string, IGTFPortModel>, IReadOnlyList<IGTFPortModel>
    {
        Dictionary<string, IGTFPortModel> m_Dictionary;
        List<int> m_Order;
        List<IGTFPortModel> m_PortModels;

        public OrderedPorts(int capacity = 0)
        {
            m_Dictionary = new Dictionary<string, IGTFPortModel>(capacity);
            m_Order = new List<int>(capacity);
            m_PortModels = new List<IGTFPortModel>(capacity);
        }

        public void Add(IGTFPortModel portModel)
        {
            m_Dictionary.Add(portModel.UniqueName, portModel);
            m_PortModels.Add(portModel);
            m_Order.Add(m_Order.Count);
        }

        public bool Remove(IGTFPortModel portModel)
        {
            bool found = false;
            if (m_Dictionary.ContainsKey(portModel.UniqueName))
            {
                m_Dictionary.Remove(portModel.UniqueName);
                found = true;
                int index = m_PortModels.FindIndex(x => x == portModel);
                m_PortModels.Remove(portModel);
                m_Order.Remove(index);
                for (int i = 0; i < m_Order.Count; ++i)
                {
                    if (m_Order[i] > index)
                        --m_Order[i];
                }
            }
            return found;
        }

        public void SwapPortsOrder(IGTFPortModel a, IGTFPortModel b)
        {
            int indexA = m_PortModels.IndexOf(a);
            int indexB = m_PortModels.IndexOf(b);
            int oldAOrder = m_Order[indexA];
            m_Order[indexA] = m_Order[indexB];
            m_Order[indexB] = oldAOrder;
        }

        #region IReadOnlyDictionary implementation
        public IEnumerator<KeyValuePair<string, IGTFPortModel>> GetEnumerator() => m_Dictionary.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => m_Dictionary.Count;
        public bool ContainsKey(string key) => m_Dictionary.ContainsKey(key);

        public bool TryGetValue(string key, out IGTFPortModel value)
        {
            return m_Dictionary.TryGetValue(key, out value);
        }

        public IGTFPortModel this[string key] => m_Dictionary[key];

        public IEnumerable<string> Keys => m_Dictionary.Keys;
        public IEnumerable<IGTFPortModel> Values => m_Dictionary.Values;
        #endregion IReadOnlyDictionary implementation

        #region IReadOnlyList<IGTFPortModel> implementation
        IEnumerator<IGTFPortModel> IEnumerable<IGTFPortModel>.GetEnumerator()
        {
            Assert.AreEqual(m_Order.Count, m_PortModels.Count, "these lists are supposed to always be of the same size");
            return m_Order.Select(i => m_PortModels[i]).GetEnumerator();
        }

        public IGTFPortModel this[int index] => m_PortModels[m_Order[index]];
        #endregion IReadOnlyList<IGTFPortModel> implementation
    }
}
