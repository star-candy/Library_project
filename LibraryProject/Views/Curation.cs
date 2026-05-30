using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LibraryProject.Models;
using LibraryProject.Services;

namespace LibraryProject.Views
{
    public class Curation : Form
    {
        private FlowLayoutPanel flowRecommended;
        private Panel pnlChartBase;
        private Label lblLoading;
        public event EventHandler<BookItem> CurationLoanRequested;

        public Curation()
        {
            this.Text = "AI 맞춤 도서 큐레이션";
            this.Size = new Size(720, 700);
            this.StartPosition = FormStartPosition.CenterParent;

            Label lblChart = new Label 
            { 
                Text = "나의 독서 스펙트럼 (대출 통계)", 
                Location = new Point(30, 20), 
                AutoSize = true, 
                Font = new Font("Noto Sans KR", 14F, FontStyle.Bold) 
            };

            pnlChartBase = new Panel 
            { 
                Location = new Point(30, 55), 
                Size = new Size(640, 200), 
                BorderStyle = BorderStyle.None, 
                BackColor = Color.FromArgb(248, 249, 250) 
            };

            pnlChartBase.Paint += PnlChartBase_Paint;

            Label lblRecommend = new Label 
            { 
                Text = "이 책은 어떠세요?", 
                Location = new Point(30, 265), 
                AutoSize = true, 
                Font = new Font("Noto Sans KR", 14F, FontStyle.Bold) 
            };

            Label lblRecommendDes = new Label
            {
                Text = "AI 추천 도서 - 클릭 시 즉시 대출",
                Location = new Point(32, 298),
                AutoSize = true,
                Font = new Font("Noto Sans KR", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 110, 120)
            };

            flowRecommended = new FlowLayoutPanel 
            { 
                Location = new Point(30, 325), 
                Size = new Size(640, 290), 
                AutoScroll = true 
            };

            // 로딩 안내 오버레이
            lblLoading = new Label
            {
                Text = "대출 이력을 기반으로 맞춤 큐레이션을 생성 중입니다...\n잠시만 기다려주세요.",
                Font = new Font("Noto Sans KR", 11, FontStyle.Regular),
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(30, 50),
                Size = new Size(640, 565), // 상단 타이틀을 제외한 나머지 영역만 덮도록 고정
                BackColor = Color.FromArgb(240)
            };

            this.Controls.AddRange(new Control[] { lblChart, pnlChartBase, lblRecommend, lblRecommendDes,flowRecommended });
            this.Controls.Add(lblLoading);
            lblLoading.BringToFront();
        }

        public void SetLoadingStatus(bool isLoading)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetLoadingStatus(isLoading)));
                return;
            }

            if (lblLoading != null)
            {
                if (isLoading)
                {
                    lblLoading.Visible = true;
                    lblLoading.BringToFront();
                }
                else
                {
                    lblLoading.Visible = false;
                    lblLoading.SendToBack(); // 데이터를 보여주기 위해 완전히 뒤로 밀어버림
                }
            }
        }

        private List<LoanRecord> _history = new List<LoanRecord>();

        public void DisplayStatistics(List<LoanRecord> history)
        {
            _history = history;
            pnlChartBase.Invalidate();
        }

        private void PnlChartBase_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            if (_history == null || _history.Count == 0)
            {
                e.Graphics.DrawString("대출 이력이 없습니다.", new Font("Arial", 10), Brushes.Black, new PointF(10, 10));
                return;
            }

            var categoryCounts = _history
                .Where(h => !string.IsNullOrEmpty(h.CategoryName))
                .GroupBy(h => 
                {
                    var parts = h.CategoryName.Split('>');
                    return parts.Length > 1 ? parts[1].Trim() : parts[0].Trim();
                })
                .ToDictionary(g => g.Key, g => g.Count());

            if (categoryCounts.Count == 0) return;

            int maxCount = categoryCounts.Values.Max();
            int margin = 25;
            int barHeight = Math.Max(20, (pnlChartBase.Height - margin * 2) / categoryCounts.Count - 5);
            int currentY = margin;
            int maxBarWidth = pnlChartBase.Width - 280;

            foreach (var kvp in categoryCounts)
            {
                int barWidth = maxCount == 0 ? 0 : (int)((double)kvp.Value / maxCount * maxBarWidth);

                // 파스텔 컬러 적용
                using (Brush barBrush = new SolidBrush(Color.FromArgb(114, 182, 230)))
                {
                    g.FillRectangle(barBrush, 160, currentY, barWidth, barHeight);
                }

                using (Font font = new Font("Noto Sans KR", 9))
                {
                    g.DrawString(kvp.Key, font, Brushes.Black, new RectangleF(10, currentY, 140, barHeight), new StringFormat { Alignment = StringAlignment.Far, LineAlignment = System.Drawing.StringAlignment.Center });
                    g.DrawString(kvp.Value.ToString() + "권", font, Brushes.DimGray, new PointF(165 + barWidth, currentY + barHeight / 2 - 7));
                }

                currentY += barHeight + 8;
            }
        }

        public void DisplayRecommendations(List<BookItem> books)
        {
            flowRecommended.Controls.Clear();

            if (books == null || books.Count == 0)
            {
                Label lblEmpty = new Label { Text = "현재 조건에 맞는 AI 추천 도서가 존재하지 않습니다.", AutoSize = true, ForeColor = Color.Gray, Font = new Font("Noto Sans KR", 10), Margin = new Padding(20) };
                flowRecommended.Controls.Add(lblEmpty);
                return;
            }

            foreach (var book in books)
            {
                // 개별 도서 카드형 버튼 프레임 빌드
                Button btnCard = new Button
                {
                    Size = new Size(190, 115),
                    Margin = new Padding(10),
                    BackColor = Color.FromArgb(250, 250, 251),
                    Cursor = Cursors.Hand
                };
                btnCard.FlatAppearance.BorderColor = Color.FromArgb(222, 226, 230);
                btnCard.FlatAppearance.BorderSize = 1;

                // 카드 내 요소 배치 (제목, 저자, 출판사) - 자식 요소가 클릭을 막지 않도록 Enabled = false 처리
                Label lblTitle = new Label { Text = book.Title, Location = new Point(10, 12), Size = new Size(170, 45), Font = new Font("Noto Sans KR", 9.5F, FontStyle.Bold), ForeColor = Color.Black, AutoEllipsis = true, Enabled = false };
                Label lblAuthor = new Label { Text = book.Author, Location = new Point(10, 62), Size = new Size(170, 20), Font = new Font("Noto Sans KR", 8.5F), ForeColor = Color.FromArgb(64, 64, 64), AutoEllipsis = true, Enabled = false };
                Label lblPub = new Label { Text = book.Publisher ?? "알라딘 도서", Location = new Point(10, 85), Size = new Size(170, 18), Font = new Font("Noto Sans KR", 8F), ForeColor = Color.FromArgb(85, 85, 85), AutoEllipsis = true, Enabled = false };

                btnCard.Controls.AddRange(new Control[] { lblTitle, lblAuthor, lblPub });

                // 마우스 오버 시 하이라이팅 반응성 부여
                btnCard.MouseEnter += (s, e) => { btnCard.BackColor = Color.FromArgb(243, 244, 246); btnCard.FlatAppearance.BorderColor = Color.FromArgb(173, 181, 189); };
                btnCard.MouseLeave += (s, e) => { btnCard.BackColor = Color.FromArgb(250, 250, 251); btnCard.FlatAppearance.BorderColor = Color.FromArgb(222, 226, 230); };

                // 카드 클릭 시 대출 확인창 작동 연계
                btnCard.Click += (s, e) =>
                {
                    DialogResult result = MessageBox.Show($"'[{book.Title}]' \n도서를 대출하시겠습니까?", "대출 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        CurationLoanRequested?.Invoke(this, book);
                    }
                };

                flowRecommended.Controls.Add(btnCard);
            }
        }

        public void ShowMessage(string message)
        {
            MessageBox.Show(message);
        }
    }
}
