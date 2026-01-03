using OtpAuthenticator.Core.Models;

namespace OtpAuthenticator.Core.Services.Interfaces;

/// <summary>
/// OTP 계정 저장소 인터페이스
/// </summary>
public interface IAccountRepository
{
    /// <summary>
    /// 모든 계정 조회
    /// </summary>
    Task<IReadOnlyList<OtpAccount>> GetAllAsync();

    /// <summary>
    /// ID로 계정 조회
    /// </summary>
    Task<OtpAccount?> GetByIdAsync(Guid id);

    /// <summary>
    /// 계정 추가
    /// </summary>
    Task<OtpAccount> AddAsync(OtpAccount account);

    /// <summary>
    /// 계정 수정
    /// </summary>
    Task UpdateAsync(OtpAccount account);

    /// <summary>
    /// 계정 삭제
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// 정렬 순서 업데이트
    /// </summary>
    Task UpdateSortOrderAsync(IEnumerable<(Guid Id, int SortOrder)> orders);

    /// <summary>
    /// 계정 개수
    /// </summary>
    Task<int> GetCountAsync();

    /// <summary>
    /// 즐겨찾기 계정 조회
    /// </summary>
    Task<IReadOnlyList<OtpAccount>> GetFavoritesAsync();

    /// <summary>
    /// 폴더별 계정 조회
    /// </summary>
    Task<IReadOnlyList<OtpAccount>> GetByFolderAsync(Guid? folderId);

    /// <summary>
    /// 미분류 계정 조회
    /// </summary>
    Task<IReadOnlyList<OtpAccount>> GetUncategorizedAsync();

    // Folder operations

    /// <summary>
    /// 모든 폴더 조회
    /// </summary>
    Task<IReadOnlyList<OtpFolder>> GetAllFoldersAsync();

    /// <summary>
    /// 폴더 추가
    /// </summary>
    Task<OtpFolder> AddFolderAsync(OtpFolder folder);

    /// <summary>
    /// 폴더 수정
    /// </summary>
    Task UpdateFolderAsync(OtpFolder folder);

    /// <summary>
    /// 폴더 삭제 (계정은 미분류로 이동)
    /// </summary>
    Task DeleteFolderAsync(Guid id);

    /// <summary>
    /// 폴더 내 계정 수 조회
    /// </summary>
    Task<int> GetAccountCountInFolderAsync(Guid? folderId);
}
