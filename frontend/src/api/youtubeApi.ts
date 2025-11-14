import axios from 'axios';

export type YoutubeComment = {
  authorDisplayName: string;
  authorChannelUrl: string;
  commentText: string;
  publishedAt: string;
};

export type YoutubeCommentsResponse = {
  videoId: string;
  page: number;
  pageSize: number;
  hasMore: boolean;
  comments: YoutubeComment[];
};

const baseURL = (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? 'http://localhost:5063/api';

const client = axios.create({
  baseURL,
  timeout: 1000 * 15
});

export const fetchYoutubeComments = async (videoId: string, page: number): Promise<YoutubeCommentsResponse> => {
  const response = await client.get<YoutubeCommentsResponse>('/youtubecomments', {
    params: { videoId, page }
  });

  return response.data;
};
