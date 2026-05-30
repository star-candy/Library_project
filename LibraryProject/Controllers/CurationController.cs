using System;
using System.Net.Http;
using LibraryProject.Views;
using LibraryProject.Services;

namespace LibraryProject.Controllers
{
    // 추천 도서 뷰(Curation) 이벤트를 관장하는 컨트롤러
    public class CurationController
    {
        // 큐레이션 뷰의 인스턴스
        private Curation _view;
        // 도서 추천 서비스 연결용 인스턴스
        private RecommendationService _recommendationService;
        private AladinApiService _apiService;
        // 대출 내역 조회 등을 위한 라이브러리 서비스 인스턴스
        private LibraryService _libraryService;
        private Models.User _user;
        //대출 내역 업데이트를 위한 유저뷰
        private UserView _userView;

        // CurationController 생성자
        public CurationController(LibraryService libraryService, Models.User user = null, UserView userView = null)
        {
            _user = user;
            // 외부에서 주입받은 libraryService를 내부에 저장
            _libraryService = libraryService;
            _apiService = new AladinApiService(new HttpClient());
            // Aladin API를 이용해 도서 추천 시스템 서비스 초기화
            _recommendationService = new RecommendationService(_apiService);
            _userView = userView;
        }

        // Curation 화면을 비동기적으로 보여주는 메서드
        public async void ShowCurationView()
        {
            // 큐레이션 뷰 생성
            _view = new Curation();

            _view.CurationLoanRequested += View_LoanRequested;
            // 화면 띄우기
            _view.Show();

            // 추천 로직 수행 중 오류 방지를 위한 try-catch
            try
            {
                _view.SetLoadingStatus(true);
                // 라이브러리 서비스에서 이전 대출 내역(History)을 가져옴
                System.Collections.Generic.List<Models.LoanRecord> history;
                if (_user != null)
                {
                    history = Models.LoanRecord.GetLoansByUser(_user.UserId);

                    foreach (var record in history)
                    {
                        if (long.TryParse(record.Isbn13, out long isbn13))
                        {
                            try
                            {
                                var response = await _apiService.GetBookInfoAsync(isbn13);
                                if (response != null && response.BookItems != null && response.BookItems.Count > 0)
                                {
                                    record.CategoryName = response.BookItems[0].CategoryName;
                                }
                            }
                            catch
                            {
                                // 무시하고 다음으로 진행
                            }
                        }
                    }
                }
                else
                {
                    history = _libraryService.GetLoanHistory();
                }
                
                // 조회된 기록을 통계 데이터 형태로 뷰에 표시
                _view.DisplayStatistics(history);

                // 가져온 대출 내역을 바탕으로 비동기로 추천 도서 목록을 가져옴
                var recommendations = await _recommendationService.GetRecommendationsAsync(history);
                // 뷰에 추천 도서 목록을 전달하여 출력
                _view.DisplayRecommendations(recommendations);
            }
            // 예외 발생 시의 catch
            catch (Exception ex)
            {
                // 실패 메시지와 예외 메시지를 뷰에 띄워줌
                _view.ShowMessage($"추천 도서를 가져오는 중 오류가 발생했습니다: {ex.Message}");
            }
            finally
            {
                _view.SetLoadingStatus(false);
            }
        }
        private void View_LoanRequested(object sender, BookItem book)
        {
            // 오류가 발생할 수 있는 로직을 try-catch 블록으로 감쌈
            try
            {
                if (_user != null)
                {
                    var bookDb = new Models.Book(
                        book.Isbn13,
                        book.Title,
                        book.Author,
                        book.Publisher,
                        null,
                        book.CategoryId,
                        book.CoverLink,
                        book.Description
                    );
                    Models.LoanRecord.InsertLoan(_user.UserId, bookDb, System.DateTime.Now.AddDays(14));
                    _view.ShowMessage($"'{book.Title}' 이(가) 대출되었습니다.");
                    if(_userView != null)
                        _userView.DisplayLoans(Models.LoanRecord.GetLoansByUser(_user.UserId).FindAll(l => l.ReturnDate == null));
                }
                else
                {
                    _libraryService.LoanBook(book);
                    _view.ShowMessage($"'{book.Title}' 이(가) 대출되었습니다.");
                    if(_userView != null)
                        _userView.DisplayLoans(_libraryService.GetCurrentLoans());
                }
            }
            // 에러 발생 시 예외 객체 포획
            catch (System.Exception ex)
            {
                // 에러 메시지를 뷰에 표시
                _view.ShowMessage(ex.Message);
            }
        }
    }
}
