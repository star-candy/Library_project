using System;
using System.Net.Http;
using LibraryProject.Views;
using LibraryProject.Services;

namespace LibraryProject.Controllers
{
    public class UserController
    {
        // 사용자 인터페이스 뷰 인스턴스 
        private UserView _view;
        // 외부 도서 API 서비스 연동 인스턴스
        private AladinApiService _apiService;
        // 도서관 도서 대출/반납 관련 비즈니스 로직 서비스 인스턴스
        private LibraryService _libraryService;
        private Models.User _user;

        // UserController 생성자
        public UserController(Models.User user = null)
        {
            _user = user;
            // HttpClient를 이용해 API 서비스 생성
            _apiService = new AladinApiService(new HttpClient());
            // 라이브러리 서비스 생성
            _libraryService = new LibraryService();
        }

        // 사용자 뷰를 화면에 표시하는 메서드
        public void ShowUserView()
        {
            // 뷰 인스턴스 초기화
            _view = new UserView();

            // 뷰에서 발생한 SearchRequested 이벤트를 담당 메서드에 연결
            _view.SearchRequested += View_SearchRequested;
            // 뷰에서 발생한 CurationRequested 이벤트를 담당 메서드에 연결
            _view.CurationRequested += View_CurationRequested;
            // 뷰에서 발생한 LoanRequested 이벤트를 담당 메서드에 연결
            _view.LoanRequested += View_LoanRequested;
            // 뷰에서 발생한 ReturnRequested 이벤트를 담당 메서드에 연결
            _view.ReturnRequested += View_ReturnRequested;
            
            if (_user != null)
            {
                _view.DisplayLoans(Models.LoanRecord.GetLoansByUser(_user.UserId).FindAll(l => l.ReturnDate == null));
            }

            // 뷰 화면을 띄움
            _view.Show();
        }

        // 도서 대출 요청 시 실행되는 이벤트 핸들러
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
                    _view.ShowMessage($"'{book.Title}'\n이(가) 대출되었습니다.");
                    _view.DisplayLoans(Models.LoanRecord.GetLoansByUser(_user.UserId).FindAll(l => l.ReturnDate == null));
                }
                else
                {
                    _libraryService.LoanBook(book);
                    _view.ShowMessage($"'{book.Title}'\n이(가) 대출되었습니다.");
                    _view.DisplayLoans(_libraryService.GetCurrentLoans());
                }
            }
            // 에러 발생 시 예외 객체 포획
            catch (System.Exception ex)
            {
                // 에러 메시지를 뷰에 표시
                _view.ShowMessage(ex.Message);
            }
        }

        // 도서 반납 요청 시 실행되는 이벤트 핸들러
        private void View_ReturnRequested(object sender, string isbn13)
        {
            try
            {
                if (_user != null)
                {
                    var loans = Models.LoanRecord.GetLoansByUser(_user.UserId);
                    var recordToReturn = loans.Find(l => l.Isbn13 == isbn13 && l.ReturnDate == null);
                    if (recordToReturn != null)
                    {
                        Models.LoanRecord.ReturnBook(recordToReturn.LoanId);
                        _view.ShowMessage("도서가 반납되었습니다.");
                        _view.DisplayLoans(Models.LoanRecord.GetLoansByUser(_user.UserId).FindAll(l => l.ReturnDate == null));
                    }
                }
                else
                {
                    _libraryService.ReturnBook(isbn13);
                    _view.ShowMessage("도서가 반납되었습니다.");
                    _view.DisplayLoans(_libraryService.GetCurrentLoans());
                }
            }
            catch (System.Exception ex)
            {
                _view.ShowMessage(ex.Message);
            }
        }

        // 도서 검색 요청 시 비동기로 실행되는 이벤트 핸들러
        private async void View_SearchRequested(object sender, string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return;

            try
            {
                var response = await _apiService.GetBooksByQuery(query);
                if (response != null && response.BookItems != null)
                {
                    _view.DisplayBooks(response.BookItems);
                }
            }
            catch (System.Exception ex)
            {
                _view.ShowMessage($"도서 검색 중 오류가 발생했습니다: {ex.Message}");
            }
        }

        // 도서 추천(Curation) 요청 시 실행되는 이벤트 핸들러
        private void View_CurationRequested(object sender, System.EventArgs e)
        {
            var curationController = new CurationController(_libraryService, _user, _view);
            curationController.ShowCurationView();
        }
    }
}
