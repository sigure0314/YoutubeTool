import { FormEvent, useCallback, useMemo, useState } from 'react';
import dayjs from 'dayjs';
import { fetchYoutubeComments, YoutubeComment } from '../api/youtubeApi';
import Pagination from './Pagination';
import './YoutubeCommentsTool.css';

const PAGE_SIZE = 40;

const extractVideoId = (input: string): string | null => {
  const trimmed = input.trim();
  if (!trimmed) {
    return null;
  }

  const directIdMatch = trimmed.match(/^[a-zA-Z0-9_-]{11}$/);
  if (directIdMatch) {
    return directIdMatch[0];
  }

  const patterns = [
    /v=([a-zA-Z0-9_-]{11})/,
    /youtu\.be\/([a-zA-Z0-9_-]{11})/,
    /shorts\/([a-zA-Z0-9_-]{11})/
  ];

  for (const pattern of patterns) {
    const match = trimmed.match(pattern);
    if (match?.[1]) {
      return match[1];
    }
  }

  return null;
};

const YoutubeCommentsTool = () => {
  const [videoInput, setVideoInput] = useState('');
  const [videoId, setVideoId] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [comments, setComments] = useState<YoutubeComment[]>([]);
  const [hasMore, setHasMore] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleFetch = useCallback(
    async (targetVideoId: string, targetPage: number) => {
      setIsLoading(true);
      setError(null);
      try {
        const response = await fetchYoutubeComments(targetVideoId, targetPage);
        setComments(response.comments);
        setHasMore(response.hasMore);
        setPage(response.page);
        setVideoId(response.videoId);
      } catch (err) {
        console.error(err);
        setError('無法取得留言，請確認影片 ID 與後端服務設定。');
        setComments([]);
        setHasMore(false);
      } finally {
        setIsLoading(false);
      }
    },
    []
  );

  const handleSubmit = useCallback(
    async (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();
      const parsedId = extractVideoId(videoInput);
      if (!parsedId) {
        setError('請輸入有效的影片 ID 或網址');
        return;
      }

      await handleFetch(parsedId, 1);
    },
    [handleFetch, videoInput]
  );

  const handlePageChange = useCallback(
    async (nextPage: number) => {
      if (!videoId) {
        return;
      }

      await handleFetch(videoId, nextPage);
    },
    [handleFetch, videoId]
  );

  const totalCountLabel = useMemo(() => {
    if (!comments.length) {
      return '尚未載入留言';
    }

    return `本頁顯示 ${comments.length} 筆留言 (每頁 ${PAGE_SIZE} 筆)`;
  }, [comments.length]);

  return (
    <section className="youtube-tool">
      <header className="youtube-tool__header">
        <h2>YouTube 影片主要留言撈取</h2>
        <p>只撈取主留言且同一使用者僅顯示一次，留言文字超過 50 字將自動截斷。</p>
      </header>

      <form className="youtube-tool__form" onSubmit={handleSubmit}>
        <label className="youtube-tool__label" htmlFor="video-input">
          影片網址或 ID
        </label>
        <div className="youtube-tool__input-group">
          <input
            id="video-input"
            type="text"
            value={videoInput}
            onChange={(event) => setVideoInput(event.target.value)}
            placeholder="例如：https://www.youtube.com/watch?v=XXXXXXXXXXX"
            autoComplete="off"
          />
          <button type="submit" disabled={isLoading}>
            {isLoading ? '載入中…' : '開始撈取'}
          </button>
        </div>
      </form>

      {error && <div className="youtube-tool__error">{error}</div>}

      {comments.length > 0 && (
        <div className="youtube-tool__summary">{totalCountLabel}</div>
      )}

      <div className="youtube-tool__results">
        {isLoading && <div className="youtube-tool__loading">資料載入中…</div>}
        {!isLoading && comments.length === 0 && !error && (
          <div className="youtube-tool__placeholder">請輸入影片資訊後開始撈取。</div>
        )}
        {!isLoading && comments.length > 0 && (
          <table className="youtube-tool__table">
            <thead>
              <tr>
                <th>留言時間</th>
                <th>留言者</th>
                <th>留言內容 (最多 50 字)</th>
              </tr>
            </thead>
            <tbody>
              {comments.map((comment, index) => (
                <tr key={`${comment.authorChannelUrl}-${index}`}>
                  <td>{dayjs(comment.publishedAt).format('YYYY/MM/DD HH:mm')}</td>
                  <td>
                    <a href={comment.authorChannelUrl} target="_blank" rel="noreferrer">
                      {comment.authorDisplayName}
                    </a>
                  </td>
                  <td>{comment.commentText}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {comments.length > 0 && (
        <Pagination page={page} hasMore={hasMore} onPageChange={handlePageChange} isLoading={isLoading} />
      )}
    </section>
  );
};

export default YoutubeCommentsTool;
