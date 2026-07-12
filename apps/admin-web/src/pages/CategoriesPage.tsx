import { createCategory, getCategories, updateCategory } from '../lib/api';
import { ManagementPage } from './ManagementPage';

export function CategoriesPage() {
  return (
    <ManagementPage
      title="Kategoriler"
      subtitle="Şikayet kategorisi yönetimi"
      itemLabel="Kategori"
      showScope
      load={getCategories}
      create={createCategory}
      update={updateCategory}
    />
  );
}
