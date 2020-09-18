using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace DecryptSkidsomware
{

    class Program
    {

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool CryptHashData(IntPtr hHash, byte[] pbData, uint dataLen, uint flags);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool CryptHashData(IntPtr hHash, IntPtr pbData, uint dataLen, uint flags);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptDeriveKey(IntPtr hProv, int Algid, IntPtr hBaseData, int flags, ref IntPtr phKey);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool CryptDestroyHash(IntPtr hHash);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool CryptDestroyKey(IntPtr phKey);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CryptDecrypt(IntPtr hKey, IntPtr hHash, bool Final, uint dwFlags, byte[] pbData, ref uint pdwDataLen);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool CryptCreateHash(IntPtr hProv, uint algId, IntPtr hKey, uint dwFlags, ref IntPtr phHash);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CryptAcquireContext(ref IntPtr hProv, string pszContainer, string pszProvider, uint dwProvType, uint dwFlags);


        const int prov_rsa_aes = 24;
        const uint crypt_verifycontext = 0xF0000000;
        const int crypt_exportable = 1;
        const int crypt_userdata = 1;
        const uint alg_md5 = 32771;
        const uint alg_des = 26113;
        const int alg_userkey = 0;
        

        static IntPtr crypt_context;

        static IntPtr crypt_startup()
        {
            crypt_context = new IntPtr();
            CryptAcquireContext(ref crypt_context, "", "", prov_rsa_aes, crypt_verifycontext);
            return crypt_context;
        }

        static IntPtr crypt_derivekey(byte[] password, uint alg_id, uint hash_alg_id = alg_md5)
       
        {
            IntPtr output_hash_context = new IntPtr();
            IntPtr output_handle = new IntPtr();

            CryptCreateHash(crypt_context, hash_alg_id, IntPtr.Zero, 0, ref output_hash_context);

            CryptHashData(output_hash_context, password, (uint)password.Length, crypt_userdata);

            CryptDeriveKey(crypt_context, (int)alg_id, output_hash_context, crypt_exportable, ref output_handle);

            return output_handle;
        }

        static IntPtr crypt_derivekey(IntPtr password, uint alg_id, uint hash_alg_id = alg_md5)
        {
            IntPtr output_hash_context = new IntPtr();
            IntPtr output_handle = new IntPtr();

            CryptCreateHash(crypt_context, hash_alg_id, IntPtr.Zero, 0, ref output_hash_context);

            CryptHashData(output_hash_context, password, (uint)8, crypt_userdata);

            CryptDeriveKey(crypt_context, (int)alg_id, output_hash_context, crypt_exportable, ref output_handle);

            return output_handle;
        }

        static byte[] crypt_decryptdata(byte[] data, IntPtr key, uint alg_id, bool final)
        {

            IntPtr derived_key_ptr = new IntPtr();
            if (alg_id != alg_userkey)
            {
                derived_key_ptr = crypt_derivekey(key, alg_id);
            }
            else
            {
                derived_key_ptr = key;
            }

            uint output_size = (uint)data.Length;
            CryptDecrypt(derived_key_ptr, IntPtr.Zero, final, 0, data, ref output_size);
            byte[] output_buffer = new byte[output_size];
            Array.Copy(data, output_buffer, output_size);

            return output_buffer;
        }
        static void crypt_decryptfile(String sourcefile, String destinationfile, byte[] cryptkey, uint alg_id)
        {
            IntPtr derived_key_ptr = new IntPtr();
            if(alg_id != alg_userkey)
            {
                derived_key_ptr = crypt_derivekey(cryptkey, alg_id);
            }

            FileStream fin = File.OpenRead(sourcefile);
            FileStream fout = File.OpenWrite(destinationfile);

            
            while(true)
            {
                int remaining_size = (int)fin.Length - (int)fin.Position;
                if (remaining_size > 1024 * 1024)
                {
                    byte[] temp_buffer = new byte[1024 * 1024];
                    fin.Read(temp_buffer, 0x00, temp_buffer.Length);
                    byte[] output_data = crypt_decryptdata(temp_buffer, derived_key_ptr, alg_userkey, false);
                    fout.Write(output_data, 0x00, output_data.Length);
                }
                else
                {
                    byte[] temp_buffer = new byte[remaining_size];
                    fin.Read(temp_buffer, 0x00, temp_buffer.Length);
                    byte[] output_data = crypt_decryptdata(temp_buffer, derived_key_ptr, alg_userkey, true);
                    fout.Write(output_data, 0x00, output_data.Length);
                    break;
                }
            }

            fout.Close();
            fin.Close();

        }

        [STAThread]
        static void Main(string[] args)
        {
            crypt_startup();

            OpenFileDialog fbd = new OpenFileDialog();
           
            fbd.Filter = "ALJROL Ransom'd Files|Lock.*";
            fbd.ShowDialog();
            string dir_path = Path.GetDirectoryName(fbd.FileName);
            string file_name = Path.GetFileName(fbd.FileName);
            if (file_name.StartsWith("Lock."))
            {
                string original_name = file_name.Substring(5, file_name.Length - 5);
                string output_path = Path.Combine(dir_path, original_name);
                crypt_decryptfile(fbd.FileName, output_path, Encoding.UTF8.GetBytes("888"), alg_des);
                MessageBox.Show("Decrypted!", "File has been decrypted!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
                MessageBox.Show("Not ransomed!", "Not encrypted by the ALJROL Ransomware.", MessageBoxButtons.OK, MessageBoxIcon.Error);

        }
    }
}
