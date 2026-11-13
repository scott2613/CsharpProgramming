using ComputerStore.Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace ComputerStore.API.Security;
public sealed class AccountErrorMiddleware(RequestDelegate next, ILogger<AccountErrorMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { }
        catch (OrderOperationException ex) when (!context.Response.HasStarted)
        { context.Response.Clear(); context.Response.StatusCode = ex.StatusCode; await context.Response.WriteAsJsonAsync(new { message = ex.Message }); }
        catch (Exception ex) when (!context.Response.HasStarted && IsOrderConflict(ex))
        {
            logger.LogWarning(ex, "Xung đột cập nhật đơn hàng/kho."); context.Response.Clear(); context.Response.StatusCode = 409;
            await context.Response.WriteAsJsonAsync(new { message = "Dữ liệu đang được cập nhật. Vui lòng tải lại và kiểm tra trước khi thử lại." });
        }
        catch (AccountOperationException ex) when (!context.Response.HasStarted)
        { context.Response.Clear(); context.Response.StatusCode = ex.StatusCode; await context.Response.WriteAsJsonAsync(new { message = ex.Message }); }
        catch (DbUpdateException ex) when (!context.Response.HasStarted && ex.InnerException is SqlException sql && sql.Number is 2601 or 2627)
        { logger.LogWarning("Trùng thông tin tài khoản khi ghi dữ liệu."); context.Response.Clear(); context.Response.StatusCode = 409; await context.Response.WriteAsJsonAsync(new { message = "Thông tin tài khoản đã được sử dụng." }); }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            logger.LogError(ex, "Không xử lý được yêu cầu {TraceId}", context.TraceIdentifier);
            context.Response.Clear(); context.Response.StatusCode = 500;
            if (context.Request.Path.StartsWithSegments("/api")) await context.Response.WriteAsJsonAsync(new { message = "Không thể xử lý yêu cầu. Vui lòng thử lại.", traceId = context.TraceIdentifier });
            else { context.Response.ContentType = "text/plain; charset=utf-8"; await context.Response.WriteAsync("Không thể xử lý yêu cầu. Vui lòng thử lại. Mã tra cứu: " + context.TraceIdentifier); }
        }
    }
    private static bool IsOrderConflict(Exception ex)
    {
        for (Exception? e = ex; e != null; e = e.InnerException)
            if (e is SqlException sql && sql.Number is 1205 or 1222) return true;
        return ex is DbUpdateConcurrencyException;
    }
}
