import { useState } from 'react';
import Menu, { MenuItem } from './components/Menu';
import YoutubeCommentsTool from './components/YoutubeCommentsTool';
import './styles/app.css';

const tools: MenuItem[] = [
  {
    id: 'youtube-comments',
    title: 'YouTube 留言撈取',
    description: '取得指定影片的主要留言 (每頁 40 筆)' 
  }
];

const App = () => {
  const [activeToolId, setActiveToolId] = useState<string>(tools[0]?.id ?? '');

  return (
    <div className="app">
      <aside className="app__sidebar">
        <h1 className="app__logo">YouTube 工具箱</h1>
        <Menu items={tools} activeItemId={activeToolId} onSelect={setActiveToolId} />
      </aside>
      <main className="app__content">
        {activeToolId === 'youtube-comments' && <YoutubeCommentsTool />}
      </main>
    </div>
  );
};

export default App;
