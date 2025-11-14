import './Menu.css';

export type MenuItem = {
  id: string;
  title: string;
  description?: string;
};

type MenuProps = {
  items: MenuItem[];
  activeItemId?: string;
  onSelect?: (id: string) => void;
};

const Menu = ({ items, activeItemId, onSelect }: MenuProps) => {
  return (
    <nav className="menu">
      <ul className="menu__list">
        {items.map((item) => {
          const isActive = item.id === activeItemId;
          return (
            <li key={item.id} className={`menu__item ${isActive ? 'menu__item--active' : ''}`}>
              <button
                type="button"
                className="menu__button"
                onClick={() => onSelect?.(item.id)}
              >
                <span className="menu__title">{item.title}</span>
                {item.description && <span className="menu__description">{item.description}</span>}
              </button>
            </li>
          );
        })}
      </ul>
    </nav>
  );
};

export default Menu;
