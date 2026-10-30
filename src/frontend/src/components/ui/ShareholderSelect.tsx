import { useId, useMemo, useState } from 'react';
import type { Shareholder } from '@/types';

interface ShareholderSelectProps {
  shareholders: Shareholder[];
  value: string;
  onChange: (shareholderId: string) => void;
  disabled?: boolean;
  allowNone?: boolean;
  placeholder?: string;
}

function normalizeSearch(value: string) {
  return value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLocaleLowerCase('pt-BR');
}

export function ShareholderSelect({
  shareholders,
  value,
  onChange,
  disabled = false,
  allowNone = false,
  placeholder = 'Pesquisar sócio por nome ou email...',
}: ShareholderSelectProps) {
  const listboxId = useId();
  const [search, setSearch] = useState('');
  const [isOpen, setIsOpen] = useState(false);

  const sortedShareholders = useMemo(
    () => [...shareholders].sort((a, b) => a.name.localeCompare(b.name, 'pt-BR', { sensitivity: 'base' })),
    [shareholders],
  );
  const filteredShareholders = sortedShareholders.filter((shareholder) =>
    normalizeSearch(`${shareholder.name} ${shareholder.email ?? ''}`).includes(
      normalizeSearch(search.trim()),
    ),
  );
  const selectedShareholder = shareholders.find((shareholder) => shareholder.id === value);

  return (
    <div>
      <input
        type="search"
        role="combobox"
        aria-label="Pesquisar sócio"
        aria-expanded={isOpen}
        aria-controls={listboxId}
        aria-autocomplete="list"
        placeholder={placeholder}
        className="input"
        disabled={disabled}
        value={isOpen ? search : selectedShareholder?.name ?? search}
        onFocus={() => {
          setSearch('');
          setIsOpen(true);
        }}
        onChange={(event) => {
          setSearch(event.target.value);
          setIsOpen(true);
        }}
        onKeyDown={(event) => {
          if (event.key === 'Escape') {
            setIsOpen(false);
            setSearch('');
          }
        }}
      />
      {isOpen && (
        <div
          id={listboxId}
          role="listbox"
          aria-label="Sócios"
          className="mt-1 max-h-40 overflow-y-auto rounded-md border border-primary-200 bg-white py-1"
        >
          {allowNone && (
            <button
              type="button"
              role="option"
              aria-selected={!value}
              className="w-full px-3 py-2 text-left text-sm text-primary-600 hover:bg-primary-50"
              onClick={() => {
                onChange('');
                setSearch('');
                setIsOpen(false);
              }}
            >
              Nenhum
            </button>
          )}
          {filteredShareholders.map((shareholder) => (
            <button
              key={shareholder.id}
              type="button"
              role="option"
              aria-selected={shareholder.id === value}
              className="w-full px-3 py-2 text-left text-sm hover:bg-primary-50"
              onClick={() => {
                onChange(shareholder.id);
                setSearch('');
                setIsOpen(false);
              }}
            >
              <span className="block text-primary">{shareholder.name}</span>
              {shareholder.email && (
                <span className="block text-xs text-primary-500">{shareholder.email}</span>
              )}
            </button>
          ))}
          {!filteredShareholders.length && (
            <p className="px-3 py-2 text-sm text-primary-500">Nenhum sócio encontrado</p>
          )}
        </div>
      )}
    </div>
  );
}
