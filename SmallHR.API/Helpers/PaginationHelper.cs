namespace SmallHR.API.Helpers;

public static class PaginationHelper
{
    public static (int PageNumber, int PageSize) Normalize(int? pageNumber, int? pageSize, int defaultSize = 10, int maxSize = 100)
    {
        var number = pageNumber.GetValueOrDefault(1);
        if (number < 1) number = 1;

        var size = pageSize.GetValueOrDefault(defaultSize);
        if (size < 1) size = defaultSize;
        if (size > maxSize) size = maxSize;

        return (number, size);
    }
}


