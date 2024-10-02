using System.Net;
using Common.Enums;
using Common.Exceptions;
using Common.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Postgres;
using Postgres.Abstractions;
using Postgres.Models;
using Service.DataTransferObjects.Requests;
using Service.DataTransferObjects.Responses;

namespace Service.Handlers.Queries;

public class GetQuestionByScoreQueryHandler : IRequestHandler<GetQuestionByScoreRequest, GetQuestionByScoreResponse>
{
    private readonly ILingualLoopGenericRepository<Question> _genericRepository;
    private readonly ILingualLoopGenericRepository<UserVideoHistory> _uvhRepository;
    private readonly LingualLoopContext _context;

    public GetQuestionByScoreQueryHandler(ILingualLoopGenericRepository<Question> genericRepository, ILingualLoopGenericRepository<UserVideoHistory> uvhRepository)
    {
        _genericRepository = genericRepository;
        _uvhRepository = uvhRepository;
        _context = genericRepository.GetDbContext();
    }

    public async Task<GetQuestionByScoreResponse> Handle(GetQuestionByScoreRequest request, CancellationToken cancellationToken)
    {
        var watchedVideoList = await _uvhRepository.GetListAsync(f => f.UserId.Equals(request.UserId), 
            uvh => new UserVideoHistory (){ VideoId = uvh.VideoId });
        var watchedVideoIds = watchedVideoList.Select(w => w.VideoId).ToList();
        
        var question = await _context.Questions
            .Where(q => (q.MinScore <= request.UserScore && q.MaxScore >= request.UserScore) && !watchedVideoIds.Contains(q.VideoId))
            .OrderBy(q => EF.Functions.Random())
            .Include(q => q.Answers)
            .Select(q => new GetQuestionByScoreResponse
            {
                QuestionId = q.QuestionId,
                QuestionText = q.QuestionText,
                MinScore = q.MinScore,
                MaxScore = q.MaxScore,
                Video = new Video
                {
                    VideoUrl = q.Video!.VideoUrl,
                    VideoDescription = q.Video.VideoDescription,
                    VideoTitle = q.Video.VideoTitle
                },
                VideoId = q.VideoId,
                Answers = q.Answers!.Select(a => new Answer
                {
                    AnswerId = a.AnswerId,
                    AnswerText = a.AnswerText,
                    IsCorrect = a.IsCorrect
                }).ToList() 
            })
            .FirstOrDefaultAsync(cancellationToken);
        
        if (question is null)
        {
            throw new LingualLoopException(ErrorCode.NoDataFoundInQuestions.CreateMessage(request.UserScore),
                ErrorCode.NoDataFoundInQuestions.GetDescription(request.UserScore), HttpStatusCode.BadRequest);
        }

        return question;
    }
}