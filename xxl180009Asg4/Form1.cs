using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;

/// <summary>
/// This is a HCI Assignment at UTDallas.
/// The program searches a text file for a string.
/// When user click the search button, it will find all
/// occurrences of the string in the text file and show
/// each one of them in the list. Since its a multithreded
/// program, the UI can cancel the search during the process.
/// 
/// User can only search in the text file. and can search any strings.
/// 
/// Written by Xiang Lin for CS6326.001, assignment 4, starting Ocober 11, 2018. Professor: John Cole
/// NetID: xxl180009
/// 
/// Author:Xiang Lin
/// Last Modified: 10/20/2018
/// </summary>

namespace xxl180009Asg4
{

   
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            //initialize the status bar.
            toolStripStatusLabel1.Text = "";
            toolStripStatusLabel3.Text = "";
            toolStripStatusLabel4.Text = "";
            toolStripStatusLabel5.Text = "";
            //initialize the size of components and UI.
            Size = new Size(1200, 600);
            progressBar.Value = 0;
            progressBar.Width = statusStrip3.Width - 5;
            tableLayoutPanel1.Width = ClientRectangle.Width - 60;
            listView1.Width = ClientRectangle.Width - 60;
            listView1.Height = ClientRectangle.Height - 200;
            btnClearSearch.Size = btnSearch.Size;
            btnClearAll.Size = btnBrowse.Size;

        }


        //Global bool parameter, use to go to different report in progress_changed method.
        //when found the text, report to listview, otherwise, dont report to listview.
        public bool found = false;
        //Global parameters-->_processParameter
        struct DataParameter
        {
            public String currentLineText;//text of the current line, to report progress in listview.
            public int currentLineNum;//line number to indicate.
            public int numOfLinesFound;//number of lines found.
            public int foundLineNum;//lines found.
            public int progressPercent;//use to pass percent to progress bar.
            public int lineSize;//size of the current line.
            public int fileSize;//size of the file.

        }
        private DataParameter _processParameter;



        /// <summary>
        /// btnBrowse opens the correct txt data file for searching strings. It alse handles the validation of the path and file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnBrowse_Click(object sender, EventArgs e)
        {
            clearAll();
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Text|*.txt||*.*"; //allows only text files to be selected in the openfiledialog.
            //select the data file to be analyzed. Return the directory to the textbox.
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                String fileDir = ofd.FileName;
                directory.Text = fileDir;
            }
        }
        /// <summary>
        /// click the search button to start searching the text in the file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSearchAndCancel_Click(object sender, EventArgs e)
        {
            //When click the process, toggle the button to cancel.
            if (btnSearch.Text == "Search")
            {
                //do the work of search here.
                clearListViewAndInitializeStatus();
                //Disable the open file button and textbox when work started.
                directory.Enabled = false;
                btnBrowse.Enabled = false;
                destString.Enabled = false;
                btnClearAll.Enabled = false;
                btnClearSearch.Enabled = false;
                //Do the Work of process here...
                btnSearch.Text = "Cancel";
                //need to check if the thread is busy, should not run again when its running.
                //cannot run multiple threads concurrently.
                if (!backgroundWorker1.IsBusy)
                {
                    backgroundWorker1.RunWorkerAsync();
                    //toolStripStatusLabel3.Text = "Searching... ";
                }

            }
            else if (btnSearch.Text == "Cancel")
            {
                //Do the Work of Cancel here.
                btnSearch.Text = "Search";
                destString.Select();

                //only allow cancel when the thread is working, otherwise doesnt make sense.
                if (backgroundWorker1.IsBusy)
                {
                    backgroundWorker1.CancelAsync();

                }
                else
                {
                    toolStripStatusLabel1.Text = "thread is not processing, doesnt make sense to cancel.";
                }
            }
        }
        /// <summary>
        /// Handle clear form click.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClear_Click(object sender, EventArgs e)
        {
            clearAll();
        }
        private void btnClearSearch_Click(object sender, EventArgs e)
        {
            destString.Text = "";
            clearListViewAndInitializeStatus();
        }
        /// <summary>
        /// helper method to clear the listview and initialize status Bar 
        /// when file directory or target search string changed 
        /// or user clicked the search again
        /// </summary>
        private void clearListViewAndInitializeStatus()
        {
            toolStripStatusLabel1.Text = "";
            toolStripStatusLabel3.Text = "";
            toolStripStatusLabel4.Text = "";
            toolStripStatusLabel5.Text = "";
            progressBar.Value = 0;
            if (listView1.Items.Count == 0)
            {
                return;
            }
            else
            {
                listView1.Items.Clear();
            }
        }
        /// <summary>
        /// This function clears all textboxes and listview items.
        /// </summary>
        private void clearAll()
        {
            directory.Text = "";
            destString.Text = "";
            clearListViewAndInitializeStatus();
        }

        /// <summary>
        /// make user easier to search, enter int the search tetbox to search quickly.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void destString_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnSearchAndCancel_Click(this, new EventArgs());
            }
        }





        //---------------------------------------------------------------
        //------------------Background Worker------------------------------
        //---------------------------------------------------------------
        /// <summary>
        /// BackgroundWorker creates a different thread to do the search work!
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// DoWork do the main search on this different thread. whenever report the progree,
        /// it will pass to progresschanged function.
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            // initialize the currentline whenever we click the process.
            _processParameter.numOfLinesFound = 0;
            _processParameter.currentLineNum = 1;
            _processParameter.foundLineNum = 0;
            _processParameter.progressPercent = 0;
            _processParameter.lineSize = 0;
            // Read the file to a string, and write each line to ListView.				
            String line = String.Empty;

            //get the file size, don't need to go through everything to get the percentage.
            FileInfo fi = new FileInfo(directory.Text);
            _processParameter.fileSize = (int)fi.Length;
            //Console.WriteLine("File Size in Bytes: {0}", _processParameter.fileSize);


            // Open the text file using a stream reader.
            using (StreamReader sr = new StreamReader(directory.Text))
            {
                //look through the file.
                while ((line = sr.ReadLine()) != null)
                {

                    //make it interesting! sleep 1 millisecond everytime you read a line.
                    Thread.Sleep(1);

                    //when read a line, found is false
                    found = false;

                    //update the current all line size, use to get percentage of progress.
                    _processParameter.lineSize = _processParameter.lineSize + (int)line.Length + 1;
                    backgroundWorker1.ReportProgress(_processParameter.currentLineNum);
                   
                    //handle the case insensitivety.
                    bool contains = (line.IndexOf(destString.Text, StringComparison.OrdinalIgnoreCase) >= 0);
                    
                    //if we find the line contains the text.
                    if (contains == true)
                    {
                        //if the line contains the search text
                        //when find a line containe target string, found is true.
                        found = true;
                        //store the line content.
                        _processParameter.currentLineText = line;
                        //increment the number of lines been found.
                        _processParameter.numOfLinesFound++;
                        //pass the line number to currently found line number.
                        _processParameter.foundLineNum = _processParameter.currentLineNum;
                        //report progress-->show on the listview.
                        backgroundWorker1.ReportProgress(_processParameter.currentLineNum);
                    }
                    //when cancel clicked, do cancel.
                    if (backgroundWorker1.CancellationPending)
                    {
                        e.Cancel = true;
                        return;
                    }
                    //increment the line number
                    _processParameter.currentLineNum++;
                }
            }
            //return line number
            e.Result = _processParameter.currentLineNum;
        }

        /// <summary>
        /// progress changed function reports the progress of this thread. When find the
        ///string been searched in a line, boolean found is true and we report the text of this line
        ///to the listview.Otherwise, increment line, update pregress bar,
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

            string lineNum = e.ProgressPercentage.ToString();
            //if not find the target string in the line, simply update the progress.
            if (found == false)
            {
                //Handle progress bar value
                float pct = ((float)_processParameter.lineSize / (float)_processParameter.fileSize) * 100;
                Console.WriteLine("curent percentile is: " + pct);
                progressBar.Value = (int)pct;

                toolStripStatusLabel3.Text = "Searching at line: " + lineNum + "...";
            }
            else if (found == true)
            {
                //Handle progress bar value
                float pct = ((float)_processParameter.lineSize / (float)_processParameter.fileSize) * 100;
                Console.WriteLine("curent percentile is: " + pct);
                progressBar.Value = (int)pct;

                //if find the target string, update in the listview and return.
                //When found the text in the file, Making progress here.
                //update status with the finding lines.
                toolStripStatusLabel4.Text = "Found '" + destString.Text + "' at line: " + lineNum + ".";

                //update to listview.
                ListViewItem lv = new ListViewItem(lineNum);
                lv.SubItems.Add(_processParameter.currentLineText);
                listView1.Items.Add(lv);
                Thread.Sleep(1);
                listView1.Items[listView1.Items.Count - 1].EnsureVisible();
                
                //Application.DoEvents();
                
            }

        }

        /// <summary>
        /// progress completed method handles three situations when:
        /// 
        /// 1.user click the cancel, the search canceld which means 
        /// the thread is closed and buttons are enabled and user can start a new search.
        /// User will get the current information about what have been searched.
        /// 
        /// 2.handle the error message.
        /// 
        /// 3.file been searched and work completed. user will get the final report of the search.
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {//Status shows the current line when canceled.
                toolStripStatusLabel3.Text = "Search Canceled. ";
                toolStripStatusLabel4.Text = "The file is been searched to line: " + _processParameter.currentLineNum + ".";
                toolStripStatusLabel5.Text = " Found " + _processParameter.numOfLinesFound + " items.";
                //Enable the open file button and textbox when work canceled.
                btnSearch.Text = "Search";
                directory.Enabled = true;
                btnBrowse.Enabled = true;
                destString.Enabled = true;
                btnClearAll.Enabled = true;
                btnClearSearch.Enabled = true;
                //change the color of progress turn to a different color.
                progressBar.Value = 0;
            }
            else if (e.Error != null)
            {
                toolStripStatusLabel4.Text = e.Error.Message;
            }
            else
            {
                if (progressBar.Value != 100)
                {
                    progressBar.Value = 100;
                }
                //Enable the open file button and textbox when work finished.
                directory.Enabled = true;
                btnBrowse.Enabled = true;
                destString.Enabled = true;
                btnClearAll.Enabled = true;
                btnClearSearch.Enabled = true;
                btnSearch.Text = "Search";
                //if nothing is found through the file, update status
                if (_processParameter.numOfLinesFound == 0)
                {
                    toolStripStatusLabel3.Text = "Search Completed. ";
                    toolStripStatusLabel4.Text = "String '" + destString.Text + "' is not found in the file";
                }
                else
                {
                    //return the last line number found the text and finish the search.
                    toolStripStatusLabel3.Text = "Search Completed. ";
                    toolStripStatusLabel4.Text = "The file is processed and the last line contains the string is " + _processParameter.foundLineNum + ".";
                    toolStripStatusLabel5.Text = " Found " + _processParameter.numOfLinesFound + " items total.";
                }
            }
        }



        //---------------------------------------------------------------
        //------------------Validation Part------------------------------
        //---------------------------------------------------------------
        /// <summary>
        /// Method to check is it ready to search.
        /// enable the search button if its ready otherwise, disable the search button.
        /// </summary>
        private void checkSearchStatus()
        {
            //if search textbox is empty or invalid path input, search button should not be valid.
            if (string.IsNullOrWhiteSpace(destString.Text))
            {
                btnSearch.Enabled = false;
                toolStripStatusLabel1.Text = "Please enter valid input.";
                toolStripStatusLabel1.Visible = true;
            }
            else if (pathIsValid(directory.Text) == false)
            {
                btnSearch.Enabled = false;
            }
            else
            {
                btnSearch.Enabled = true;
            }
        }
        /// <summary>
        /// Simple handle to validate the path of the directory. 
        /// Return true if the path ends with ".txt"
        /// Return false otherwise.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private bool pathIsValid(string input)
        {
            if (directory.Text.Length < 4)
            {
                toolStripStatusLabel1.Text = "The file path is not valid.";
                toolStripStatusLabel1.Visible = true;
                return false;
            }
            var pathEnds = directory.Text.Substring(directory.Text.Length - 4);
            if (pathEnds != ".txt")
            {
                toolStripStatusLabel1.Text = "The file path is not valid.";
                toolStripStatusLabel1.Visible = true;
                return false;
            }
            else
            {
                toolStripStatusLabel1.Text = "Ready to Search..";
                toolStripStatusLabel1.Visible = true;
                return true;
            }
        }
        /// <summary>
        /// Following methods are used to changed the status to give user indication.
        /// This two events happen when mouse stops and rests over the button.
        /// ->Show the information for users about the button.
        /// And when mouse leaves the button.
        /// ->Clear the information on the status bar.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnBrowse_MouseMove(object sender, MouseEventArgs e)
        {
            toolStripStatusLabel1.Text = "Open a .txt file to start search.";
            toolStripStatusLabel1.Visible = true;
        }
        private void btnBrowse_MouseLeave(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "";
        }
        private void btnSearch_MouseMove(object sender, MouseEventArgs e)
        {
            if (btnSearch.Text == "Search")
            {
                //Console.WriteLine("progress bar value is:  " + progressBar.Value);
                if (toolStripStatusLabel3.Text == "Search Canceled. " || toolStripStatusLabel3.Text == "Search Completed. ")
                {
                    toolStripStatusLabel1.Text = "Start a new search. Current found will lost.";
                }
                else
                {
                    toolStripStatusLabel1.Text = "Search strings in the file.";
                }
            }
            else
            {

                toolStripStatusLabel1.Text = "Cancel and stop the current search.";
            }
        }
        private void btnSearch_MouseLeave(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "";
        }
        /// <summary>
        /// checks the value in the textbox to enable the search button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox2_CursorChanged(object sender, EventArgs e)
        {
            checkSearchStatus();
        }
        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            clearListViewAndInitializeStatus();
            checkSearchStatus();
        }
        private void textBox2_Click(object sender, EventArgs e)
        {
            checkSearchStatus();
        }
        private void directory_TextChanged(object sender, EventArgs e)
        {
            clearListViewAndInitializeStatus();
            checkSearchStatus();
        }
        private void directory_MouseMove(object sender, MouseEventArgs e)
        {
            toolStripStatusLabel1.Text = "Choose a valid text file to start search.";
            toolStripStatusLabel1.Visible = true;
        }
        private void directory_MouseLeave(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "";
        }
        //this is the search textbox contains the target string.
        private void textBox2_MouseMove(object sender, MouseEventArgs e)
        {
            toolStripStatusLabel1.Text = "Put valid string to start search.";
            toolStripStatusLabel1.Visible = true;
        }
        //this is the search textbox contains the target string.
        private void textBox2_MouseLeave(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "";
        }
        //When the size of status strip changed, change the size of progressBar.
        private void statusStrip3_SizeChanged(object sender, EventArgs e)
        {
            progressBar.Width = statusStrip3.Width - 5;
        }
        //When size of form changed, all components changed size accordingly.
        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            progressBar.Width = statusStrip3.Width - 5;
            tableLayoutPanel1.Width = ClientRectangle.Width - 60;
            listView1.Width = ClientRectangle.Width - 60;
            listView1.Height = ClientRectangle.Height - 200;
            btnClearAll.Size = btnBrowse.Size;
            btnClearSearch.Size = btnSearch.Size;
        }
        private void btnClear_MouseMove(object sender, MouseEventArgs e)
        {
            toolStripStatusLabel1.Text = "Clear the form.";
        }
        private void btnClear_MouseLeave(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "";
        }

        private void btnClearSearch_MouseMove(object sender, MouseEventArgs e)
        {
            toolStripStatusLabel1.Text = "Clear the search box and listview.";
        }

        private void btnClearSearch_MouseLeave(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "";
        }
    }

    public static class StringExtensions
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }
    }


}
