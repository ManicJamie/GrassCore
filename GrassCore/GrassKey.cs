using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace GrassCore
{
    /*
    This struct is more or less lifted from itsjohncs/flibber-hk/GrassyKnight, with various unnecessary methods stripped & restructured slightly for ease of use.
    */
#pragma warning disable CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
    public readonly struct GrassKey
    {
        public readonly string SceneName;
        public readonly string ObjectName;
        public readonly Vector2 Position;

        [JsonConstructor]
        public GrassKey(string sceneName, string objectName, Vector2 Pos)
        {
            SceneName = sceneName;
            ObjectName = objectName;
            Position = Pos;
        }
        public GrassKey(string SceneName, string ObjectName, float X, float Y) : this(SceneName, ObjectName, new Vector2(X, Y)) { }
        /// <summary>
        /// Creates a key from a GameObject. Note that Position is downcast to a Vector2, discarding Z height.
        /// </summary>
        public GrassKey(GameObject go) : this(go.scene.name, go.name, (Vector2)go.transform.position) { }

        public override int GetHashCode() => (SceneName, ObjectName, Position).GetHashCode();
        public override string ToString() => $"{SceneName}/{ObjectName} ({Position.x}, {Position.y})";

        public static bool operator ==(GrassKey left, GrassKey right) => left.Equals(right);

        public static bool operator !=(GrassKey left, GrassKey right) => !left.Equals(right);

        // The size of the arrays Serialize returns and Deserialize expects
        public const int NumSerializationTokens = 4;

        // Encodes into UTF-16 (which should be a no-op since that's how
        // strings are backed) and then converts to Base64. In the Remarks
        // section of https://docs.microsoft.com/en-us/dotnet/api/system.convert.tobase64string?view=net-5.0
        // it describes the alphabet used. Notably does not include `;`.
        private static string ToBase64(string str)
        {
            return Convert.ToBase64String(
                // Read "Unicode" as UTF-16
                Encoding.Unicode.GetBytes(str));
        }

        // Decodes a base 64 string into what should be valid UTF-16 which we
        // then convert to a string (which should be a no-op for the same
        // reason as above).
        private static string StringFromBase64(string str)
        {
            // Read "Unicode" as UTF-16
            return Encoding.Unicode.GetString(
                Convert.FromBase64String(str));
        }

        private static string ToBase64(float num)
        {
            byte[] bytes = BitConverter.GetBytes(num);
            if (BitConverter.IsLittleEndian)
            {
                // Ensure the bytes are in big-endian order
                Array.Reverse(bytes);
            }

            return Convert.ToBase64String(bytes);
        }

        private static float FloatFromBase64(string str)
        {
            byte[] bytes = Convert.FromBase64String(str);
            if (BitConverter.IsLittleEndian)
            {
                // The serialized bytes are always big endian, so we gotta flip
                // them back if we're on a little endian machine
                Array.Reverse(bytes);
            }

            return BitConverter.ToSingle(bytes, 0);
        }

        public string[] Serialize()
        {
            return new string[] {
                ToBase64(SceneName),
                ToBase64(ObjectName),
                ToBase64(Position.x),
                ToBase64(Position.y),
            };
        }

        public static GrassKey Deserialize(string[] serialized)
        {
            if (serialized.Length != NumSerializationTokens)
            {
                throw new ArgumentException(
                    $"Got {serialized.Length} tokens for " +
                    $"GrassKey.Deserialize. Expected " +
                    $"{NumSerializationTokens}.");
            }

            return new GrassKey(
                StringFromBase64(serialized[0]),
                StringFromBase64(serialized[1]),
                new Vector2(
                    FloatFromBase64(serialized[2]),
                    FloatFromBase64(serialized[3])));
        }

        public static GrassKey FromSerializedString(string serialized)
        {
            return Deserialize(serialized.Split(';'));
        }
    }
}
