using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [PublicAPI]
    public class OrderedPorts : IReadOnlyDictionary<string, IPortModel>, IReadOnlyList<IPortModel>
    {
        Dictionary<string, IPortModel> m_Dictionary;
        List<int> m_Order;
        List<IPortModel> m_PortModels;

        public OrderedPorts(int capacity = 0)
        {
            m_Dictionary = new Dictionary<string, IPortModel>(capacity);
            m_Order = new List<int>(capacity);
            m_PortModels = new List<IPortModel>(capacity);
        }

        public void Add(IPortModel portModel)
        {
            m_Dictionary.Add(portModel.UniqueName, portModel);
            m_PortModels.Add(portModel);
            m_Order.Add(m_Order.Count);
        }

        public bool Remove(IPortModel portModel)
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

        public void SwapPortsOrder(IPortModel a, IPortModel b)
        {
            int indexA = m_PortModels.IndexOf(a);
            int indexB = m_PortModels.IndexOf(b);
            int oldAOrder = m_Order[indexA];
            m_Order[indexA] = m_Order[indexB];
            m_Order[indexB] = oldAOrder;
        }

        #region IReadOnlyDictionary implementation
        public IEnumerator<KeyValuePair<string, IPortModel>> GetEnumerator() => m_Dictionary.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => m_Dictionary.Count;
        public bool ContainsKey(string key) => m_Dictionary.ContainsKey(key);

        public bool TryGetValue(string key, out IPortModel value)
        {
            return m_Dictionary.TryGetValue(key, out value);
        }

        public IPortModel this[string key] => m_Dictionary[key];

        public IEnumerable<string> Keys => m_Dictionary.Keys;
        public IEnumerable<IPortModel> Values => m_Dictionary.Values;
        #endregion IReadOnlyDictionary implementation

        #region IReadOnlyList<IPortModel> implementation
        IEnumerator<IPortModel> IEnumerable<IPortModel>.GetEnumerator()
        {
            Assert.AreEqual(m_Order.Count, m_PortModels.Count, "these lists are supposed to always be of the same size");
            return m_Order.Select(i => m_PortModels[i]).GetEnumerator();
        }

        public IPortModel this[int index] => m_PortModels[m_Order[index]];
        #endregion IReadOnlyList<IPortModel> implementation
    }
}
