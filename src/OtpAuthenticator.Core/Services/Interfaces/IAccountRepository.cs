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
}
