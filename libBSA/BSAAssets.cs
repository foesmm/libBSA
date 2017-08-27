using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace org.foesmm.libBSA
{
    public class BSAAssets<TValue> : IDictionary<UInt64, TValue>, IXmlSerializable
        where TValue : BSAAsset
    {
        protected SortedDictionary<UInt64, TValue> innerDictionary;

        protected UInt32 _capacity;
        public UInt32 Capacity { get { return Math.Max(_capacity, (UInt32)innerDictionary.Count); } set { _capacity = value; } }

        public BSAAssets()
        {
            innerDictionary = new SortedDictionary<ulong, TValue>();
        }

        public TValue this[ulong key] { get => innerDictionary[key]; set => ((IDictionary<ulong, TValue>)innerDictionary)[key] = value; }

        public ICollection<ulong> Keys => ((IDictionary<ulong, TValue>)innerDictionary).Keys;

        public ICollection<TValue> Values => ((IDictionary<ulong, TValue>)innerDictionary).Values;

        public int Count => ((IDictionary<ulong, TValue>)innerDictionary).Count;

        public bool IsReadOnly => ((IDictionary<ulong, TValue>)innerDictionary).IsReadOnly;

        public void Add(TValue value)
        {
            this[value.NameHash] = value;
        }

        public void Add(ICollection<TValue> values)
        {
            foreach (var value in values)
            {
                Add(value);
            }
        }

        public void Add(ulong key, TValue value)
        {
            this[key] = value;
        }

        public void Add(KeyValuePair<ulong, TValue> item)
        {
            ((IDictionary<ulong, TValue>)innerDictionary).Add(item);
        }

        public void Clear()
        {
            ((IDictionary<ulong, TValue>)innerDictionary).Clear();
        }

        public bool Contains(KeyValuePair<ulong, TValue> item)
        {
            return ((IDictionary<ulong, TValue>)innerDictionary).Contains(item);
        }

        public bool ContainsKey(ulong key)
        {
            return ((IDictionary<ulong, TValue>)innerDictionary).ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<ulong, TValue>[] array, int arrayIndex)
        {
            ((IDictionary<ulong, TValue>)innerDictionary).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<ulong, TValue>> GetEnumerator()
        {
            return ((IDictionary<ulong, TValue>)innerDictionary).GetEnumerator();
        }

        public bool Remove(TValue value)
        {
            return Remove(value.NameHash);
        }

        public bool Remove(ulong key)
        {
            return ((IDictionary<ulong, TValue>)innerDictionary).Remove(key);
        }

        public bool Remove(KeyValuePair<ulong, TValue> item)
        {
            return ((IDictionary<ulong, TValue>)innerDictionary).Remove(item);
        }

        public TValue Get(ulong key)
        {
            return innerDictionary.TryGetValue(key, out TValue value) ? value : default(TValue);
        }

        public TValue Get(ulong key, TValue defaultValue)
        {
            return innerDictionary.TryGetValue(key, out TValue value) ? value : AddInternal(defaultValue);
        }

        public TValue Get(ulong key, Func<TValue> defaultValueProvider)
        {
            return innerDictionary.TryGetValue(key, out TValue value) ? value : AddInternal(defaultValueProvider());
        }

        public bool TryGetValue(ulong key, out TValue value)
        {
            return ((IDictionary<ulong, TValue>)innerDictionary).TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IDictionary<ulong, TValue>)innerDictionary).GetEnumerator();
        }

        protected TValue AddInternal(TValue value)
        {
            Add(value);
            return value;
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            var serializer = new XmlSerializer(typeof(TValue));

            bool wasEmpty = reader.IsEmptyElement;
            reader.Read();

            if (wasEmpty) return;

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                TValue value = (TValue)serializer.Deserialize(reader);
                Add(value);
            }
            reader.ReadEndElement();
        }

        public void WriteXml(XmlWriter writer)
        {
            var serializer = new XmlSerializer(typeof(TValue));

            foreach (var item in Values)
            {
                serializer.Serialize(writer, item);
            }
        }
    }
}
