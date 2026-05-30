using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using LibraryProject.Services;

namespace LibraryProject.Views
{
    public class UserView : Form
    {
        // 1. 멤버 변수 선언
        private TabControl tabControl1;
        private TabPage tabLoan;
        private TabPage tabReturn;

        private TextBox txtSearch;
        private DataGridView dgvBooks;
        private DataGridView dgvLoans;

        // View -> Controller 이벤트 정의
        public event EventHandler<string> SearchRequested;
        public event EventHandler CurationRequested;
        public event EventHandler<BookItem> LoanRequested;
        public event EventHandler<string> ReturnRequested;

        public UserView()
        {
            // 2. UI 초기화 함수 호출
            SetupLayout();

            this.Text = "사용자 서비스 - 도서 대출/반납";
            this.Size = new Size(600, 450);
            this.StartPosition = FormStartPosition.CenterScreen;

            // 3. 프로세스 종료 이벤트 (잠김 현상 방지)
            this.FormClosed += (s, e) => Application.Exit();
        }

        private void SetupLayout()
        {
            this.Font = new Font("Noto Sans KR", 9F, FontStyle.Regular);

            tabControl1 = new TabControl { Dock = DockStyle.Fill };
            tabLoan = new TabPage("도서 대출");
            tabReturn = new TabPage("도서 반납");

            // 도서 검색 텍스트박스와 버튼
            txtSearch = new TextBox { Location = new Point(20, 20), Width = 300, Font = new Font("Noto Sans KR", 10F), BorderStyle = BorderStyle.FixedSingle };
            Button btnSearch = new Button { Text = "검색", Location = new Point(330, 20), Size = new Size(95, 30), BackColor = Color.FromArgb(240, 242, 245), Font = new Font ("Noto Sans KR Medium", 10F, FontStyle.Bold) };
            btnSearch.Click += (s, e) => {
                SearchRequested?.Invoke(this, txtSearch.Text);
            };

            // 검색 결과 DataGridView
            dgvBooks = new DataGridView 
            { 
                Location = new Point(20, 60), 
                Size = new Size(530, 220),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };

            tabLoan.Controls.Add(txtSearch);
            tabLoan.Controls.Add(btnSearch);
            tabLoan.Controls.Add(dgvBooks);

            // 대출 버튼
            Button btnLoanAction = new Button
            {
                Text = "선택 도서 대출",
                Location = new Point(430, 300),
                Size = new Size(120, 40),
                Font = new Font("Noto Sans KR", 9.5F, FontStyle.Bold),
                BackColor = Color.FromArgb(220, 53, 69),
                Cursor = Cursors.Hand
            };
            btnLoanAction.Click += (s, e) => {
                if (dgvBooks.SelectedRows.Count > 0)
                {
                    var book = (BookItem)dgvBooks.SelectedRows[0].DataBoundItem;
                    LoanRequested?.Invoke(this, book);
                }
                else if (dgvBooks.SelectedCells.Count > 0)
                {
                    int rowIndex = dgvBooks.SelectedCells[0].RowIndex;
                    var book = (BookItem)dgvBooks.Rows[rowIndex].DataBoundItem;
                    LoanRequested?.Invoke(this, book);
                }
                else
                {
                    MessageBox.Show("대출할 도서를 선택하세요.");
                }
            };
            tabLoan.Controls.Add(btnLoanAction);

            // 추천 도서 버튼
            Button btnGoCuration = new Button 
            { 
                Text = "추천 도서 보기", 
                Location = new Point(20, 300), 
                Size = new Size(120, 40),
                BackColor = Color.FromArgb(40, 167, 69),
                Font = new Font("Noto Sans KR", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnGoCuration.Click += (s, e) => {
                CurationRequested?.Invoke(this, EventArgs.Empty);
            };

            tabLoan.Controls.Add(btnGoCuration);
            tabControl1.TabPages.Add(tabLoan);
            tabControl1.TabPages.Add(tabReturn);
            this.Controls.Add(tabControl1);

            // 반납 탭 UI 구성
            dgvLoans = new DataGridView 
            { 
                Location = new Point(20, 20), 
                Size = new Size(530, 260),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            tabReturn.Controls.Add(dgvLoans);

            Button btnReturnAction = new Button 
            { 
                Text = "선택 도서 반납", 
                Location = new Point(430, 300), 
                Size = new Size(120, 40),
                Font = new Font("Noto Sans KR", 9.5F, FontStyle.Bold),
                BackColor = Color.FromArgb(220, 53, 69),
                Cursor = Cursors.Hand
            };
            btnReturnAction.Click += (s, e) => {
                if (dgvLoans.SelectedRows.Count > 0)
                {
                    var record = (LibraryProject.Models.LoanRecord)dgvLoans.SelectedRows[0].DataBoundItem;
                    ReturnRequested?.Invoke(this, record.Isbn13);
                }
                else if (dgvLoans.SelectedCells.Count > 0)
                {
                    int rowIndex = dgvLoans.SelectedCells[0].RowIndex;
                    var record = (LibraryProject.Models.LoanRecord)dgvLoans.Rows[rowIndex].DataBoundItem;
                    ReturnRequested?.Invoke(this, record.Isbn13);
                }
                else
                {
                    MessageBox.Show("반납할 도서를 선택하세요.");
                }
            };
            tabReturn.Controls.Add(btnReturnAction);

            tabControl1.SelectedIndexChanged += (s, e) => {
                if (tabControl1.SelectedTab == tabReturn)
                {
                    // Trigger an event to refresh loans if needed, or simply expose a method
                }
            };
        }

        // Controller에서 호출할 메서드: 검색 결과 표시
        public void DisplayBooks(List<BookItem> books)
        {
            dgvBooks.DataSource = null;
            dgvBooks.DataSource = books;

            // [UI 개선]
            if (dgvBooks.Columns.Count > 0)
            {
                // 1. 일반 사용자에게 노출할 필요가 없는 내부용/데이터용 열들은 숨김 처리
                if (dgvBooks.Columns["CoverLink"] != null) dgvBooks.Columns["CoverLink"].Visible = false;
                if (dgvBooks.Columns["Description"] != null) dgvBooks.Columns["Description"].Visible = false;
                if (dgvBooks.Columns["CategoryId"] != null) dgvBooks.Columns["CategoryId"].Visible = false;
                if (dgvBooks.Columns["CategoryName"] != null) dgvBooks.Columns["CategoryName"].Visible = false;

                // 2. 영문 컬럼 -> 한국어 타이틀로 매핑
                if (dgvBooks.Columns["Isbn13"] != null) dgvBooks.Columns["Isbn13"].HeaderText = "ISBN (도서번호)";
                if (dgvBooks.Columns["Title"] != null) dgvBooks.Columns["Title"].HeaderText = "도서 제목";
                if (dgvBooks.Columns["Author"] != null) dgvBooks.Columns["Author"].HeaderText = "저자 / 작가";
                if (dgvBooks.Columns["Publisher"] != null) dgvBooks.Columns["Publisher"].HeaderText = "출판사";
            }
        }

        public void DisplayLoans(List<LibraryProject.Models.LoanRecord> loans)
        {
            dgvLoans.DataSource = null;
            dgvLoans.DataSource = loans;

            if (dgvLoans.Columns.Count > 0)
            {
                // 1. 시스템 관리용 고유 ID 및 불필요한 열들은 가리기 처리
                if (dgvLoans.Columns["LoanId"] != null) dgvLoans.Columns["LoanId"].Visible = false;
                if (dgvLoans.Columns["UserId"] != null) dgvLoans.Columns["UserId"].Visible = false;
                if (dgvLoans.Columns["ReturnDate"] != null) dgvLoans.Columns["ReturnDate"].Visible = false;
                if (dgvLoans.Columns["CategoryName"] != null) dgvLoans.Columns["CategoryName"].Visible = false;

                // 2. 영문 컬럼명을 직관적인 한글 헤더 타이틀로 매핑
                if (dgvLoans.Columns["Isbn13"] != null) dgvLoans.Columns["Isbn13"].HeaderText = "ISBN (도서번호)";
                if (dgvLoans.Columns["Title"] != null) dgvLoans.Columns["Title"].HeaderText = "도서 제목";
                if (dgvLoans.Columns["Author"] != null) dgvLoans.Columns["Author"].HeaderText = "저자 / 작가";
                if (dgvLoans.Columns["LoanDate"] != null) dgvLoans.Columns["LoanDate"].HeaderText = "대출 일자";
                if (dgvLoans.Columns["DueDate"] != null) dgvLoans.Columns["DueDate"].HeaderText = "반납 기한";

                // 3. [UX 핵심 개선] '오전/오후... 분/초'를 없애고 날짜가 100% 보이도록 yyyy-MM-dd 규격 포맷팅 주입
                if (dgvLoans.Columns["LoanDate"] != null)
                    dgvLoans.Columns["LoanDate"].DefaultCellStyle.Format = "yyyy-MM-dd";

                if (dgvLoans.Columns["DueDate"] != null)
                {
                    dgvLoans.Columns["DueDate"].DefaultCellStyle.Format = "yyyy-MM-dd";
                    // 반납 기한은 중요하므로 강조를 위해 붉은색 계열로 글자 색 변경 효과 추가
                    dgvLoans.Columns["DueDate"].DefaultCellStyle.ForeColor = Color.FromArgb(220, 53, 69);
                }

                // 4. [레이아웃 보완] 도서명과 날짜 컬럼의 채우기 비중(FillWeight)을 조절하여 여백 밸런스 균형 제어
                if (dgvLoans.Columns["Title"] != null) dgvLoans.Columns["Title"].FillWeight = 140; // 제목 공간 대폭 확보
                if (dgvLoans.Columns["LoanDate"] != null) dgvLoans.Columns["LoanDate"].FillWeight = 100;
                if (dgvLoans.Columns["DueDate"] != null) dgvLoans.Columns["DueDate"].FillWeight = 100;
                if (dgvLoans.Columns["Isbn13"] != null) dgvLoans.Columns["Isbn13"].FillWeight = 90;
                if (dgvLoans.Columns["Author"] != null) dgvLoans.Columns["Author"].FillWeight = 90;
            }
        }

        public void ShowMessage(string message)
        {
            MessageBox.Show(message);
        }
    }
}
