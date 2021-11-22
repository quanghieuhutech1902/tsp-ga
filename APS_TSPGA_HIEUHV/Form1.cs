using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Security;
using APS_TSPGA_HIEUHV.Models;
using Point = APS_TSPGA_HIEUHV.Models.Point;

namespace APS_TSPGA_HIEUHV
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private int numberOfVertices;
        private List<Point> listOfVertices = new List<Point>();
        private int tournamentSize = 5;
        private float parentPoolRatio = (float)0.67;//số cá thể tốt nhất của quần thể được dùng để crossover
        private float savedMemberRatio = (float)0.1;//số cá thể cũ được đưa sang quần thể mới
        private Random rnd = new Random();
        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void btnInput_Click(object sender, EventArgs e)
        {
            string inputFileName = "";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    inputFileName = openFileDialog1.FileName;
                }
                catch (SecurityException ex)
                {
                    MessageBox.Show("Lỗi đọc file! Vui lòng thử lại","Lỗi đọc file",MessageBoxButtons.OK,MessageBoxIcon.Error);
                }
            } 
            //xóa dữ liệu cũ
            numberOfVertices = 0;
            txtnumberV.Text = numberOfVertices.ToString();
            listOfVertices.Clear();
            lstInput.Items.Clear();

            try
            { 
                using (System.IO.StreamReader inputFile = new System.IO.StreamReader(inputFileName))
                {
                    string line;

                    //lấy số đỉnh
                    line = inputFile.ReadLine();
                    numberOfVertices = int.Parse(line);

                    //đọc tọa độ đỉnh
                    for (int i = 0; i < numberOfVertices; i++)
                    {
                        line = inputFile.ReadLine();
                        string[] point = line.Split('\t');
                        Point newPoint = new Point();
                        newPoint.X = float.Parse(point[0]);
                        newPoint.Y = float.Parse(point[1]);
                        listOfVertices.Add(newPoint);
                    }

                    //hiển thị tập đỉnh lên màn hình
                    txtnumberV.Text = numberOfVertices.ToString();
                    string[] arr = new string[3];
                    for (int i = 1; i <= numberOfVertices; i++)
                    {
                        arr[0] = i.ToString();
                        arr[1] = listOfVertices[i - 1].X.ToString();
                        arr[2] = listOfVertices[i - 1].Y.ToString();

                        ListViewItem newItem = new ListViewItem(arr);
                        lstInput.Items.Add(newItem);
                    }
                     
                }
            }
            catch (Exception ioEx)
            {
                MessageBox.Show(ioEx.Message);
            }
        }

        private void btnGA_Click(object sender, EventArgs e)
        {
            if (numberOfVertices == 0) return;
            txtfit.Text = "Calculating...";
            txtMap.Text = "";
            txtTime.Text = "0";
            this.Refresh();

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            int populationSize = int.Parse(txtSize.Text);
            int numberOfGenerations = int.Parse(txtF.Text);
            float mutationRate = float.Parse(txtG.Text);
            tournamentSize = int.Parse(txtSizeT.Text);
            parentPoolRatio = float.Parse(txtParent.Text);
            savedMemberRatio = float.Parse(txtOld.Text);

            List<Member> population = new List<Member>();

            //khởi tạo quần thể ban đầu
            CreateInitialGeneration(populationSize, population);
            for (int i = 1; i <= numberOfGenerations; i++)
            {
                CreateNextGeneration(populationSize, population, mutationRate);
            }

            //lấy ra cá thể tốt nhất của quần thể, tức fitness bé nhất, hiển thị kết quả
            int indexBest = 0;
            for (int i = 1; i < populationSize; i++)
            {
                if (population[i].fitnessValue < population[indexBest].fitnessValue)
                    indexBest = i;
            }
            string text = "";
            for (int i = 0; i < numberOfVertices; i++)
            {
                text += (population[indexBest].genes[i] + 1).ToString() + " ";
            }
            text += (population[indexBest].genes[0] + 1).ToString();

            stopWatch.Stop();
            txtMap.Text = text;
            txtfit.Text = Math.Round(population[indexBest].fitnessValue, 2).ToString();
            txtTime.Text = stopWatch.ElapsedMilliseconds.ToString();

            population.Clear();

        }


        //khởi tạo quần thể ban đầu
        private void CreateInitialGeneration(int size, List<Member> population)
        {
            int[] arr = new int[numberOfVertices];

            //khởi tạo một mảng chứa giá trị 0,1,2...,n-1
            for (int i = 0; i < numberOfVertices; i++)
            {
                arr[i] = i;
            }

            //mỗi vòng for tạo một phần tử, bằng cách random số lần hoán vị
            //mỗi lần hoán vị random ra hai vị trí i và j, hoán vị phần tử tại i và j
            for (int k = 0; k < size; k++)
            {
                int swapTimes = rnd.Next(numberOfVertices);
                for (int t = 0; t < swapTimes; t++)
                {
                    //random số i và j thỏa 0 <= i,j < độ dài chuỗi gene
                    int i = rnd.Next(0, numberOfVertices);
                    int j = rnd.Next(0, numberOfVertices);
                    //hoán vị phần từ i và phần tử j
                    int temp = arr[i];
                    arr[i] = arr[j];
                    arr[j] = temp;
                }

                //lấy chuỗi hoán vị làm phần tử mới
                //tính giá trị fitness cho phần tử mới
                Member newMember = new Member();
                newMember.genes = new int[numberOfVertices];
                for (int i = 0; i < numberOfVertices; i++)
                {
                    newMember.genes[i] = arr[i];
                }
                newMember.fitnessValue = FitnessCalculation(newMember.genes);
                population.Add(newMember);
            }

        }

        //loại bỏ những phần tử có số fitness kém nhất
        private void RemoveWorstMember(int newSize, List<Member> population)
        {
            int size = population.Count;
            //giả sử trên sử mảng đã sắp xếp có size phần tử từ 0..size-1
            //giữ lại newSize phần tử từ 0..newSize-1, phần tử từ newSize đến size-1 bị loại
            for (int k = newSize; k <= size - 1; k++)
            {
                int index = 0;
                //tìm phần tử có fitness value cao nhất, tức là tệ nhất
                for (int i = 1; i < population.Count; i++)
                {
                    if (population[i].fitnessValue > population[index].fitnessValue)
                        index = i;
                }
                //loại phần tử đó
                population.RemoveAt(index);
            }
        }

        //đưa n phần tử có fitness tốt nhất lên đầu danh sách
        private void SortPopulation(int n, List<Member> population)
        {
            for (int i = 0; i < n; i++)
            {
                int index = i;
                for (int j = i + 1; j < population.Count; j++)
                {
                    if (population[j].fitnessValue > population[index].fitnessValue)
                        index = j;
                }
                Member temp = population[index];
                population[index] = population[i];
                population[i] = temp;
            }
        }

        //đưa n cá thể ở đầu danh sách sang quần thể mới
        private void TransferToNextGeneration(int n, List<Member> population, List<Member> newPopulation)
        {
            for (int i = 0; i < n; i++)
            {
                Member newMember = new Member();
                newMember.fitnessValue = population[i].fitnessValue;
                newMember.genes = new int[numberOfVertices];
                CopyArray(population[i].genes, 0, newMember.genes, 0, numberOfVertices);
                newPopulation.Add(newMember);
            }
        }

        private void CreateNextGeneration(int size, List<Member> population, float mutationRate)
        {
            List<Member> newPopulation = new List<Member>();
            int newSize = (int)Math.Floor(size * savedMemberRatio);
            SortPopulation(newSize, population);
            TransferToNextGeneration(newSize, population, newPopulation);

            //loại bỏ những phần tử có số fitness kém nhất
            int parentSize = (int)Math.Floor(size * parentPoolRatio);
            RemoveWorstMember(parentSize, population);

            int[] tournament = new int[tournamentSize];
            int k = newPopulation.Count;

            //tạo ra các cá thể con cho quần thể mới
            while (k < size)
            {
                //chọn parent thứ nhất
                for (int i = 0; i < tournamentSize; i++)
                {
                    tournament[i] = rnd.Next(0, parentSize);
                }
                int parent_1 = tournament[0];
                for (int i = 1; i < tournamentSize; i++)
                {
                    if (population[tournament[i]].fitnessValue < population[parent_1].fitnessValue)
                        parent_1 = i;
                }

                //chọn parent thứ hai
                for (int i = 0; i < tournamentSize; i++)
                {
                    tournament[i] = rnd.Next(0, parentSize);
                }
                int parent_2 = tournament[0];
                for (int i = 1; i < tournamentSize; i++)
                {
                    if (population[tournament[i]].fitnessValue < population[parent_1].fitnessValue)
                        parent_2 = i;
                }

                if (parent_1 != parent_2)
                {
                    //tạo child
                    Member child = new Member();
                    child.genes = new int[numberOfVertices];

                    //chọn ngẫu nhiên phần tử tại vị trí 1....n-2 làm điểm crossover
                    int crossPoint = rnd.Next(1, numberOfVertices - 1);
                    //lấy gene từ 0 đến crossPoint làm phần thứ 1, từ crossPoint+1 đến n-1 làm phần thứ 2
                    int[] part_a = new int[crossPoint + 1];
                    int[] part_c = new int[crossPoint + 1];
                    CopyArray(population[parent_1].genes, 0, part_a, 0, part_a.Length);
                    CopyArray(population[parent_2].genes, 0, part_c, 0, part_c.Length);
                    //tính fitness đoạn a, c
                    double fitness_a = FitnessCalculation(part_a);
                    double fitness_c = FitnessCalculation(part_c);
                    //ghép đoạn gene a, c của hai parent vào child
                    int exist_length;
                    if (fitness_a < fitness_c)
                    {
                        exist_length = FillChild(part_a, child.genes, 0);
                        exist_length = exist_length + FillChild(part_c, child.genes, exist_length);
                    }
                    else
                    {
                        exist_length = FillChild(part_c, child.genes, 0);
                        exist_length = exist_length + FillChild(part_a, child.genes, exist_length);
                    }
                    //chuỗi gene của child chưa đủ n gene
                    if (numberOfVertices - exist_length > 0)
                    {
                        //tính fitness đoạn b, d
                        int[] part_b = new int[numberOfVertices - crossPoint - 1];
                        int[] part_d = new int[numberOfVertices - crossPoint - 1];
                        CopyArray(population[parent_1].genes, crossPoint + 1, part_b, 0, part_b.Length);
                        CopyArray(population[parent_2].genes, crossPoint + 1, part_d, 0, part_d.Length);
                        double fitness_b = FitnessCalculation(part_b);
                        double fitness_d = FitnessCalculation(part_d);
                        //ghép đoạn gene b hoặc d của hai parent vào child
                        if (fitness_b < fitness_d)
                        {
                            exist_length = exist_length + FillChild(part_b, child.genes, exist_length);
                        }
                        else
                        {
                            exist_length = exist_length + FillChild(part_d, child.genes, exist_length);
                        }
                    }

                    //đột biến
                    if (rnd.NextDouble() < mutationRate)
                        Mutation_RSM(child.genes);
                    //tính fitness của child, cho vào quần thể mới
                    child.fitnessValue = FitnessCalculation(child.genes);
                    newPopulation.Add(child);
                    k++;
                }
            }

            population.Clear();
            population.AddRange(newPopulation);
        }

        private int FillChild(int[] arr_source, int[] arr_destination, int index_destination)
        {
            int i = 0;
            int j = 0;

            while ((i < arr_source.Length)
                && (index_destination + j < arr_destination.Length))
            {
                if (Array.IndexOf(arr_destination, arr_source[i], 0, index_destination) == -1)
                {
                    arr_destination[index_destination + j] = arr_source[i];
                    j++;
                }
                i++;
            }

            //trả về số gene được thêm vào chuỗi gene
            return j;
        }

        //thực hiện đột biến trên chuỗi gene
        private void Mutation_RSM(int[] arr)
        {
            int i = rnd.Next(0, arr.Length);
            int j = rnd.Next(0, arr.Length);

            if (i < j)
            {
                while (i < j)
                {
                    int temp = arr[i];
                    arr[i] = arr[j];
                    arr[j] = temp;
                    i++;
                    j--;
                }
            }
            else
            {
                while (j < i)
                {
                    int temp = arr[i];
                    arr[i] = arr[j];
                    arr[j] = temp;
                    i--;
                    j++;
                }
            }
        }

        //tính giá trị fitness cho chuỗi gene arr
        private double FitnessCalculation(int[] arr)
        {
            double fitnessValue = 0;
            int n = arr.Length;

            //fitness từ i = 0..n-2
            for (int i = 1; i < n; i++)
            {
                float x1 = listOfVertices[arr[i - 1]].X;
                float y1 = listOfVertices[arr[i - 1]].Y;
                float x2 = listOfVertices[arr[i]].X;
                float y2 = listOfVertices[arr[i]].Y;

                fitnessValue += Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
            }

            //fitness tại i = n-1
            float x_1 = listOfVertices[arr[n - 1]].X;
            float y_1 = listOfVertices[arr[n - 1]].Y;
            float x_2 = listOfVertices[arr[0]].X;
            float y_2 = listOfVertices[arr[0]].Y;

            fitnessValue += Math.Sqrt((x_1 - x_2) * (x_1 - x_2) + (y_1 - y_2) * (y_1 - y_2));

            //trả kết quả
            return fitnessValue;
        }

        //chép mảng từ arr_source vào arr_destination, độ dài length
        private void CopyArray(int[] arr_source, int index_source, int[] arr_destination, int index_destination, int length)
        {
            int i = 0;

            while ((i < length)
                && (index_source + i < arr_source.Length)
                && (index_destination + i < arr_destination.Length))
            {
                arr_destination[index_destination + i] = arr_source[index_source + i];
                i++;
            }
        }
    }
}
