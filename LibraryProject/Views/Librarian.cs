using System.Drawing;
using System.Windows.Forms;

namespace LibraryProject.Views
{
    public class Librarian : Form
    {
        // 1. UI 컨트롤을 멤버 변수로 선언하여 관리
        private Label lblHeader;
        private DataGridView dgvOverdue;
        private Panel pnlBottom;
        private Button btnNotify;

        public event System.EventHandler NotifyRequested;

        public Librarian()
        {
            // 2. 레이아웃 설정 메서드 호출
            SetupLayout();

            this.Text = "사서 관리 모드 - 연체자 관리";
            this.Size = new Size(880, 500);
            this.StartPosition = FormStartPosition.CenterScreen;

            // 3. 폼이 닫힐 때 프로세스를 완전히 종료 (파일 잠김 방지)
            this.FormClosed += (s, e) => Application.Exit();
        }

        private void SetupLayout()
        {
            this.Font = new Font("Noto Sans KR", 9F);

            // 헤더 라벨 설정
            lblHeader = new Label
            {
                Text = "연체자 관리 대시보드",
                Font = new Font("Noto Sans KR", 13F, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 55,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // 연체자 목록 그리드뷰 설정
            dgvOverdue = new DataGridView
            {
                Dock = DockStyle.Fill,
                //BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, // 좌측 쓸모없는 고정 빈 열 숨김
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, // 행 전체 선택
                EnableHeadersVisualStyles = false,
                MultiSelect = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowTemplate = { Height = 32 }
            };
            // 데이터 바인딩 예시: dgvOverdue.DataSource = overdueList;

            // DataGridView 헤더 영역 모던 UI 스타일화
            dgvOverdue.EnableHeadersVisualStyles = false;
            dgvOverdue.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(241, 243, 245); // 평상시 헤더 배경색과 일치
            dgvOverdue.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.FromArgb(73, 80, 87);  // 평상시 헤더 글자색과 일치
            //dgvOverdue.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvOverdue.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 243, 245);
            dgvOverdue.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(73, 80, 87);
            dgvOverdue.ColumnHeadersDefaultCellStyle.Font = new Font("Noto Sans KR Medium", 9.5F, FontStyle.Bold);
            dgvOverdue.ColumnHeadersHeight = 36;

            // 하단 패널 및 버튼 설정
            pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 65 };
            
            btnNotify = new Button
            {
                Text = "연체 알림 발송",
                Font = new Font("Noto Sans KR Medium", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(220, 53, 69),
                Location = new Point(700, 15),
                Size = new Size(140, 38)
            };
            btnNotify.FlatAppearance.BorderSize = 0;

            btnNotify.Click += (s, e) => NotifyRequested?.Invoke(this, System.EventArgs.Empty);

            pnlBottom.Controls.Add(btnNotify);

            // 컨트롤 추가 (중복 방지를 위해 Clear 후 추가하거나 순서대로 추가)
            this.Controls.Clear();
            this.Controls.Add(dgvOverdue);
            this.Controls.Add(lblHeader);
            this.Controls.Add(pnlBottom);
        }

        public void DisplayOverdueLoans(System.Collections.Generic.List<Models.LoanRecord> overdueLoans)
        {
            dgvOverdue.DataSource = overdueLoans;

            if (dgvOverdue.Columns.Count > 0)
            {
                // 1. 사서 관리자 화면에서 볼 필요가 없는 열 숨김
                if (dgvOverdue.Columns["ReturnDate"] != null) dgvOverdue.Columns["ReturnDate"].Visible = false;
                if (dgvOverdue.Columns["CategoryName"] != null) dgvOverdue.Columns["CategoryName"].Visible = false;

                // 2. 컬럼명 한국어 매핑
                if (dgvOverdue.Columns["LoanId"] != null) dgvOverdue.Columns["LoanId"].HeaderText = "대출 번호";
                if (dgvOverdue.Columns["UserId"] != null) dgvOverdue.Columns["UserId"].HeaderText = "사용자 ID";
                if (dgvOverdue.Columns["Isbn13"] != null) dgvOverdue.Columns["Isbn13"].HeaderText = "ISBN (도서코드)";
                if (dgvOverdue.Columns["Title"] != null) dgvOverdue.Columns["Title"].HeaderText = "대출 도서명";
                if (dgvOverdue.Columns["Author"] != null) dgvOverdue.Columns["Author"].HeaderText = "저자 / 작가";
                if (dgvOverdue.Columns["LoanDate"] != null) dgvOverdue.Columns["LoanDate"].HeaderText = "대출 일자";
                if (dgvOverdue.Columns["DueDate"] != null) dgvOverdue.Columns["DueDate"].HeaderText = "반납 예정 기한";

                // 3. 날짜 뒤에 붙는 불필요한 시·분·초 시간 텍스트 제거 포맷 주입
                if (dgvOverdue.Columns["LoanDate"] != null)
                    dgvOverdue.Columns["LoanDate"].DefaultCellStyle.Format = "yyyy-MM-dd";

                if (dgvOverdue.Columns["DueDate"] != null)
                {
                    dgvOverdue.Columns["DueDate"].DefaultCellStyle.Format = "yyyy-MM-dd";
                    // 연체 내역이므로 가시성 극대화를 위해 반납 예정 기한 열 글자색을 붉은색으로 강조
                    dgvOverdue.Columns["DueDate"].DefaultCellStyle.ForeColor = Color.Red;
                }

                // 4. 글자 수 분포가 가장 많은 도서 제목의 가로 채우기 비중(FillWeight) 가중치 부여
                if (dgvOverdue.Columns["LoanId"] != null) dgvOverdue.Columns["LoanId"].FillWeight = 50;
                if (dgvOverdue.Columns["UserId"] != null) dgvOverdue.Columns["UserId"].FillWeight = 50;
                if (dgvOverdue.Columns["Title"] != null) dgvOverdue.Columns["Title"].FillWeight = 150;
                if (dgvOverdue.Columns["Isbn13"] != null) dgvOverdue.Columns["Isbn13"].FillWeight = 95;
                if (dgvOverdue.Columns["Author"] != null) dgvOverdue.Columns["Author"].FillWeight = 90;
                if (dgvOverdue.Columns["LoanDate"] != null) dgvOverdue.Columns["LoanDate"].FillWeight = 90;
                if (dgvOverdue.Columns["DueDate"] != null) dgvOverdue.Columns["DueDate"].FillWeight = 95;
            }
        }

        public void ShowMessage(string message)
        {
            MessageBox.Show(message, "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}