using UnityEngine;

namespace TextureArrayEssentials.Compression
{
    public static class BooleanCompression
    {
        /// <summary>
        /// Compresses the input array of bools into an int
        /// </summary>
        /// <param name="values">
        /// Bools that will be compressed
        /// </param>
        /// <returns>
        /// Compressed int of bools
        /// </returns>
        public static int CompressValues(params bool[] values)
        {
            int compressedValues = (values[0] ? 1 : 0);

            int current = 2;
            for (int i = 1; i < values.Length; i++) {
                bool value = values[i];
                compressedValues |= (value ? current : 0);

                current *= 2;
            }

            return compressedValues;
        }

        /// <summary>
        /// Retrieves all the boolean values stored in a compressed int
        /// </summary>
        /// <param name="compressedValues">
        /// Compressed int of bool values
        /// </param>
        /// <param name="valueCount">
        /// The total amount of values stored in the compressedValues
        /// </param>
        /// <returns>
        /// Array of all the values stored in the input comrpressedValues
        /// </returns>
        public static bool[] GetValues(int compressedValues, int valueCount)
        {
            bool[] values = new bool[valueCount];

            int current = 1;
            for (int i = 0; i < valueCount; i++) {
                values[i] = (compressedValues & current) != 0;
                current *= 2;
            }

            return values;
        }

        /// <summary>
        /// Overwrites a value at a given index into the input compressedValues
        /// </summary>
        /// <param name="compressedValues">
        /// Compressed int of bool values
        /// </param>
        /// <param name="index">
        /// Index that the input value will overwrite
        /// </param>
        /// <param name="value">
        /// Value inserted into the compressedValue
        /// </param>
        /// <returns>
        /// New compressed integer with the added value
        /// </returns>
        public static int AddValue(int compressedValues, int index, bool value)
        {
            int current = (int)(1 * Mathf.Pow(2, index));
            return compressedValues |= (value ? current : 0);
        }

        /// <summary>
        /// Gets a compressed value at a given index from the input compressedValues
        /// </summary>
        /// <param name="compressedValues">
        /// Compressed int of bool values
        /// </param>
        /// <param name="index">
        /// Index of which bool value will be returned
        /// </param>
        /// <returns>
        /// Value at the given index
        /// </returns>
        public static bool GetValue(int compressedValues, int index)
        {
            int current = (int)(1 * Mathf.Pow(2, index));
            return (compressedValues & current) != 0;
        }
    }
}