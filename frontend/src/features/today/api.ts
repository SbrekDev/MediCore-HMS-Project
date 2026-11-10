import { apiClient } from '../../api/axios';
import type { DashboardSummaryDto } from './types';

export async function getDashboardSummary(params: {
  from: string;
  to: string;
}): Promise<DashboardSummaryDto> {
  const response = await apiClient.get<DashboardSummaryDto>('/dashboard/summary', {
    params,
  });
  return response.data;
}
