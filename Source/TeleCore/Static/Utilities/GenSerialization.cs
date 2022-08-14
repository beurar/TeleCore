using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeleCore
{
    public static class GenSerialization
    {
        //
        public static float[] DeserializeFloat(byte[] data)
        {
            float[] result = new float[data.Length / 4];
            LoadFloat(data, result.Length, delegate (int i, float dat)
            {
                result[i] = dat;
            });
            return result;
        }

        public static void LoadFloat(byte[] dataArr, int elements, Action<int, float> writer)
        {
            if (dataArr == null || dataArr.Length == 0) return;
            for (int i = 0; i < elements; i++)
            {
                writer(i, BitConverter.ToSingle(dataArr, i * 4));
            }
        }

        //
        public static byte[] SerializeFloat(int elements, Func<int, float> reader)
        {
            byte[] array = new byte[elements * 4];
            for (int i = 0; i < elements; i++)
            {
                var bytes = BitConverter.GetBytes(reader(i));
                array[i * 4] = bytes[0];
                array[i * 4 + 1] = bytes[1];
                array[i * 4 + 2] = bytes[2];
                array[i * 4 + 3] = bytes[3];
            }
            return array;
        }

        public static byte[] SerializeFloatArray(float[] data)
        {
            return SerializeFloat(data.Length, (i) => data[i]);
        }
	}
}
