import { createFileRoute } from '@tanstack/react-router';
import { MyProfile } from '@/pages/MyProfile';

export const Route = createFileRoute('/_authenticated/my-profile')({
  component: MyProfile,
});
