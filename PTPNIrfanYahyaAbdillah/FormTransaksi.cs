using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace PTPNIrfanYahyaAbdillah
{
    public partial class FormTransaksi : Form
    {
        private SqlCommand cmd;
        private SqlDataReader dr;
        private SqlConnection conn = new SqlConnection("Data source=DESKTOP-2N25H0E\\MSSQLSERVER01;initial catalog=DB_APOTEK;integrated security=true");
        private DataTable dt;
        private int noFaktur;
        private DateTime tglFaktur;
        private List<string> kodeBarang = new List<string>();
        private List<string> namaBarang = new List<string>();
        private int hargaSatuan;
        private List<int> jmlBeli = new List<int>();
        private List<int> subTotalList = new List<int>();
        private int subTotal;
        private int total;
        private int jmlUang;
        private int kembali;

        public FormTransaksi()
        {
            InitializeComponent();
        }

        private void UpdateSubTotal()
        {
            if (string.IsNullOrWhiteSpace(textBoxJmlBarang.Text) || string.IsNullOrWhiteSpace(textBoxHargaBarang.Text))
            {
                textBoxSubTotal.Text = "";
                return;
            }

            int quantity = int.Parse(textBoxJmlBarang.Text);

            subTotal = quantity * hargaSatuan;
            textBoxSubTotal.Text = string.Format("{0:#,0}", subTotal);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            tglFaktur = DateTime.Now;
            textBoxTglFaktur.Text = tglFaktur.ToString("D");

            string comboBoxQuery = "SELECT * FROM M_BARANG";
            cmd = new SqlCommand(comboBoxQuery, conn);
            conn.Open();
            dr = cmd.ExecuteReader();
            while(dr.Read())
            {
                comboBoxIdBarang.Items.Add(dr["IdBarang"]);
            }
            conn.Close();

            dt = new DataTable();
            dt.Columns.Add("No", typeof(int));
            dt.Columns.Add("BarangId", typeof(string));
            dt.Columns.Add("Nama", typeof(string));
            dt.Columns.Add("JmlBeli", typeof(int));
            dt.Columns.Add("SubTotal", typeof(int));

            dataGridViewDetailTransaksi.DataSource = dt;

            string noFakturQuery = "SELECT COUNT(*) AS Id FROM M_TRANSAKSI";
            cmd = new SqlCommand(noFakturQuery, conn);
            conn.Open();
            object lastIdFakturObj = cmd.ExecuteScalar();
            int lastIdFaktur = 1;
            if (lastIdFakturObj != DBNull.Value) 
            {
                lastIdFaktur = Convert.ToInt32(lastIdFakturObj); 
            }
            textBoxNoFaktur.Text = $"F00{lastIdFaktur + 1}";
            conn.Close();

            string operatorQuery = "SELECT IdAdmin FROM M_ADMIN WHERE NamaAdmin=\'superadmin\'";
            cmd = new SqlCommand(operatorQuery, conn);
            conn.Open();
            string IdAdmin = cmd.ExecuteScalar().ToString();
            textBoxOperator.Text = IdAdmin;
            conn.Close();
        }

        private void comboBoxIdBarang_SelectedIndexChanged(object sender, EventArgs e)
        {
            cmd = new SqlCommand("SELECT * FROM M_BARANG WHERE IdBarang=@idBarang", conn);
            cmd.Parameters.AddWithValue("@idBarang", comboBoxIdBarang.Text);
            conn.Open();
            dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                string namaBarang = dr["NamaBarang"].ToString();
                hargaSatuan = (int) dr["HargaBarang"];

                textBoxNamaBarang.Text = namaBarang;
                textBoxHargaBarang.Text = string.Format("{0:#,0}", hargaSatuan);
            }
            conn.Close();
        }

        private void buttonExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void textBoxJmlBarang_TextChanged(object sender, EventArgs e)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(textBoxJmlBarang.Text, "[^0-9]"))
            {
                MessageBox.Show("Please enter only numbers.");
                textBoxJmlBarang.Text = textBoxJmlBarang.Text.Remove(textBoxJmlBarang.Text.Length - 1);
            }
            UpdateSubTotal();
        }

        private void buttonAddItem(object sender, EventArgs e)
        {
            dt.Columns["No"].AutoIncrement = true;
            dt.Columns["No"].AutoIncrementSeed = 1;
            dt.Columns["No"].AutoIncrementStep = 1;
            dt.Rows.Add(null, comboBoxIdBarang.Text, textBoxNamaBarang.Text, textBoxJmlBarang.Text, subTotal);
            dataGridViewDetailTransaksi.DataSource = dt;

            total += subTotal;
            textBoxTotal.Text = string.Format("{0:#,0}", total);

            kodeBarang.Add(comboBoxIdBarang?.Text);
            namaBarang.Add(textBoxNamaBarang?.Text);
            jmlBeli.Add(int.Parse(textBoxJmlBarang?.Text));
            subTotalList.Add(subTotal);
        }

        private void textBoxJmlUang_TextChanged(object sender, EventArgs e)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(textBoxJmlUang.Text, "[^0-9]"))
            {
                MessageBox.Show("Please enter only numbers.");
                textBoxJmlUang.Text = textBoxJmlUang.Text.Remove(textBoxJmlUang.Text.Length - 1);
            }

            if (string.IsNullOrWhiteSpace(textBoxJmlUang.Text) || string.IsNullOrWhiteSpace(textBoxJmlUang.Text))
            {
                textBoxSubTotal.Text = "";
                return;
            }

            jmlUang = int.Parse(textBoxJmlUang.Text);

            kembali = jmlUang - total;
            textBoxKembali.Text = string.Format("{0:#,0}", kembali);
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            conn.Open();
            SqlTransaction transaction = conn.BeginTransaction();
            try
            {
                string insertTransaksiQuery = "INSERT INTO M_TRANSAKSI (TglTransaksi, TotalJual, Dibayar, Kembali) VALUES (@TglTransaksi, @TotalJual, @Dibayar, @Kembali); SELECT SCOPE_IDENTITY();";
                SqlCommand cmd = new SqlCommand(insertTransaksiQuery, conn, transaction);
                cmd.Parameters.AddWithValue("@TglTransaksi", tglFaktur);
                cmd.Parameters.AddWithValue("@TotalJual", total);
                cmd.Parameters.AddWithValue("@Dibayar", jmlUang);
                cmd.Parameters.AddWithValue("@Kembali", kembali);

                int idTransaksi = Convert.ToInt32(cmd.ExecuteScalar());

                for (int i = 0; i < kodeBarang.Count; i++)
                {
                    string insertTransaksiDetailQuery = "INSERT INTO M_TRANSAKSIDetail (IdTransaksi, IdBarang, NamaBarang, JmlBarang, SubTotal) VALUES (@IdTransaksi, @IdBarang, @NamaBarang, @JmlBarang, @SubTotal)";
                    cmd = new SqlCommand(insertTransaksiDetailQuery, conn, transaction);
                    cmd.Parameters.AddWithValue("@IdTransaksi", idTransaksi);
                    cmd.Parameters.AddWithValue("@IdBarang", kodeBarang[i]);
                    cmd.Parameters.AddWithValue("@NamaBarang", namaBarang[i]);
                    cmd.Parameters.AddWithValue("@JmlBarang", jmlBeli[i]);
                    cmd.Parameters.AddWithValue("@SubTotal", subTotalList[i]);
                    cmd.ExecuteNonQuery();
                }

                transaction.Commit();

                noFaktur = idTransaksi + 1;
                textBoxNoFaktur.Text = $"F00{noFaktur}";

                MessageBox.Show("Transaksi Berhasil");
            }
            catch (Exception ex)
            {

                transaction.Rollback();
                MessageBox.Show("Terjadi kesalahan: " + ex.Message);
            }
            finally 
            {
                conn.Close(); 
            }

            dataGridViewDetailTransaksi.DataSource = null;
            dataGridViewDetailTransaksi.Rows.Clear();
            comboBoxIdBarang.SelectedIndex = -1;
            textBoxNamaBarang.Clear();
            textBoxHargaBarang.Clear();
            textBoxJmlBarang.Clear();
            textBoxSubTotal.Clear();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            comboBoxIdBarang.SelectedIndex = -1;
            textBoxNamaBarang.Clear();
            textBoxHargaBarang.Clear();
            textBoxJmlBarang.Clear();
            textBoxSubTotal.Clear();
        }
    }
}
