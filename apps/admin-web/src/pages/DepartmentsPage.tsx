import { createDepartment, getDepartments, updateDepartment } from '../lib/api';
import { ManagementPage } from './ManagementPage';

export function DepartmentsPage() {
  return (
    <ManagementPage
      title="Birimler"
      subtitle="Belediye birim yönetimi"
      itemLabel="Birim"
      load={getDepartments}
      create={createDepartment}
      update={updateDepartment}
    />
  );
}
