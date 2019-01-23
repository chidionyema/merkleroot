using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MerkleTree {
    class Program {
        const string blockchainApi = "https://blockchain.info/rawblock/000000000000000000155a0c59bfdb54834608e7bf55e29920fd24591f1e3a98";
        static void Main (string[] args) {
            dynamic blockData = FetchBlockData(blockchainApi);

            if (blockData == null) {
                throw new InvalidOperationException ("response failed");
            }

            var transactionHashes = new List<string> ();
            foreach (dynamic transaction in blockData.tx) {
                transactionHashes.Add (SwapAndReverse (transaction.hash));
            }

            Console.WriteLine ("Block Merkel Root: " + blockData.mrkl_root);

            Console.WriteLine ("Block Header Hash: " + blockData.hash);

            Console.WriteLine ("Nonce: " + blockData.nonce);

            string merkleRoot = BuildMerkleRoot (transactionHashes);

            Console.WriteLine ("Computed Merkel Root: " + SwapAndReverse (merkleRoot).ToLower());
        }
       private static  string BuildMerkleRoot (IList<string> merkelLeaves) {
            if (merkelLeaves == null || !merkelLeaves.Any ())

                return string.Empty;

            if (merkelLeaves.Count () == 1) {
                return merkelLeaves.First ();
            }

            if (merkelLeaves.Count () % 2 > 0) {
                merkelLeaves.Add (merkelLeaves.Last ());
            }

            var merkleBranches = new List<string> ();

            for (int i = 0; i < merkelLeaves.Count (); i += 2) {
                var leafPair= string.Concat (merkelLeaves[i], merkelLeaves[i + 1]);
                //double hash
                merkleBranches.Add (HashUsingSHA256(HashUsingSHA256 (leafPair)));
            }
            return BuildMerkleRoot (merkleBranches);
        }
        private static string HashUsingSHA256 (string data) {
            using (var sha256 = SHA256Managed.Create ()) {
                return ComputeHash (sha256, HexToByteArray (data));
            }

        }
        private static string ComputeHash (HashAlgorithm hashAlgorithm, byte[] input) {
            byte[] data = hashAlgorithm.ComputeHash (input);
            return ByteArrayToHex (data);
        }
        private static string ByteArrayToHex (byte[] bytes) {
            return BitConverter.ToString (bytes).Replace ("-", "");
        }
        private static byte[] HexToByteArray (string hex) {
            return Enumerable.Range (0, hex.Length)
                .Where (x => x % 2 == 0)
                .Select (x => Convert.ToByte (hex.Substring (x, 2), 16))
                .ToArray ();
        }
        private static string SwapAndReverse (string input) { 
            string newString = string.Empty;;
            for (int i = 0; i < input.Count (); i += 2) {
                newString += string.Concat (input[i + 1], input[i]);
            }
            return ReverseString (newString);
        }

        private static string ReverseString (string original) {
            return new string (original.Reverse ().ToArray ());
        }
        private static dynamic FetchBlockData(string path) {
            using (HttpClient client = new HttpClient ()) {
                HttpResponseMessage response = client.GetAsync (path).Result;
                if (response.IsSuccessStatusCode) {
                    var json = response.Content.ReadAsStringAsync ().Result;
                    return JsonConvert.DeserializeObject<ExpandoObject> (json);
                }
            }
            return null;
        }

    }
}