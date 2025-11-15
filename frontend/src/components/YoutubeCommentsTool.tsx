import { useCallback, useEffect, useMemo, useState } from 'react';
import dayjs from 'dayjs';
import { fetchYoutubeComments, YoutubeComment } from '../api/youtubeApi';
import './YoutubeCommentsTool.css';

const YoutubeCommentsTool = () => {
  const [videoId, setVideoId] = useState<string | null>(null);
  const [comments, setComments] = useState<YoutubeComment[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const defaultVideoId = useMemo(() => {
    const raw = import.meta.env.VITE_DEFAULT_VIDEO_ID as string | undefined;
    return raw?.trim() ?? '';
  }, []);

  const handleFetch = useCallback(
    async (targetVideoId: string) => {
      setIsLoading(true);
      setError(null);
      try {
        const response = await fetchYoutubeComments(targetVideoId, 1);
        setComments(response.comments);
        setVideoId(response.videoId);
      } catch (err) {
        console.error(err);
        setError('無法取得留言，請確認影片 ID 與後端服務設定。');
        setComments([]);
        setVideoId(null);
      } finally {
        setIsLoading(false);
      }
    },
    []
  );

  useEffect(() => {
    if (!defaultVideoId) {
      setError('尚未設定預設影片 ID，請於環境變數 VITE_DEFAULT_VIDEO_ID 指定。');
      setComments([]);
      setVideoId(null);
      return;
    }

    handleFetch(defaultVideoId);
  }, [defaultVideoId, handleFetch]);

  return (
    <section className="youtube-tool">
      <header className="youtube-tool__header">
        <h2>影片主要留言</h2>
        {videoId && <p className="youtube-tool__video-id">來源影片 ID：{videoId}</p>}
      </header>

      <div className="youtube-tool__results">
        {isLoading && <div className="youtube-tool__loading">資料載入中…</div>}
        {!isLoading && error && <div className="youtube-tool__error">{error}</div>}
        {!isLoading && !error && comments.length === 0 && (
          <div className="youtube-tool__placeholder">目前沒有留言資料。</div>
        )}
        {!isLoading && !error && comments.length > 0 && (
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
    </section>
  );
};

export default YoutubeCommentsTool;
