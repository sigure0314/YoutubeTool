import './Pagination.css';

type PaginationProps = {
  page: number;
  hasMore: boolean;
  onPageChange: (page: number) => void;
  isLoading?: boolean;
};

const Pagination = ({ page, hasMore, onPageChange, isLoading }: PaginationProps) => {
  const handlePrev = () => {
    if (page > 1) {
      onPageChange(page - 1);
    }
  };

  const handleNext = () => {
    if (hasMore) {
      onPageChange(page + 1);
    }
  };

  return (
    <div className="pagination">
      <button type="button" onClick={handlePrev} disabled={page <= 1 || isLoading}>
        上一頁
      </button>
      <span className="pagination__page">第 {page} 頁</span>
      <button type="button" onClick={handleNext} disabled={!hasMore || isLoading}>
        下一頁
      </button>
    </div>
  );
};

export default Pagination;
