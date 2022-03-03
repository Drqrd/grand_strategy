using UnityEngine;
using System.Runtime.Serialization;

// https://forum.unity.com/threads/vector3-is-not-marked-serializable.435303/

namespace Serialization.Surrogates
{
    public class ColorSerializationSurrogate : ISerializationSurrogate
    {

        // Method called to serialize a Vector3 object
        public void GetObjectData(System.Object obj, SerializationInfo info, StreamingContext context)
        {
            Color col = (Color)obj;
            info.AddValue("r", col.r);
            info.AddValue("g", col.g);
            info.AddValue("b", col.b);
            info.AddValue("a", col.a);
        }

        // Method called to deserialize a Vector3 object
        public System.Object SetObjectData(System.Object obj, SerializationInfo info,
                                           StreamingContext context, ISurrogateSelector selector)
        {
            Color col = (Color)obj;
            col.r = (float)info.GetValue("r", typeof(float));
            col.g = (float)info.GetValue("g", typeof(float));
            col.b = (float)info.GetValue("b", typeof(float));
            col.a = (float)info.GetValue("a", typeof(float));
            obj = col;
            return obj;
        }
    }
}
