import { Routes } from '@angular/router';
import { authGuard } from './auth/auth.guard';
import { Browse } from './pages/browse/browse';
import { ConversationsInbox } from './pages/conversations-inbox/conversations-inbox';
import { ConversationThread } from './pages/conversation-thread/conversation-thread';
import { ListingDetail } from './pages/listing-detail/listing-detail';
import { ListingForm } from './pages/listing-form/listing-form';
import { Login } from './pages/login/login';
import { MyListings } from './pages/my-listings/my-listings';
import { Register } from './pages/register/register';

export const routes: Routes = [
  { path: '', component: Browse },
  { path: 'login', component: Login },
  { path: 'register', component: Register },
  { path: 'listings/new', component: ListingForm, canActivate: [authGuard] },
  { path: 'listings/mine', component: MyListings, canActivate: [authGuard] },
  { path: 'listings/:id/edit', component: ListingForm, canActivate: [authGuard] },
  { path: 'listings/:id', component: ListingDetail },
  { path: 'conversations', component: ConversationsInbox, canActivate: [authGuard] },
  { path: 'conversations/:id', component: ConversationThread, canActivate: [authGuard] },
  { path: '**', redirectTo: '' },
];
