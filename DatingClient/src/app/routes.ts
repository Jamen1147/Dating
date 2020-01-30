import { Routes } from "@angular/router";
import { HomeComponent } from "./home/home.component";
import { MembersComponent } from "./members/members.component";
import { MessagesComponent } from "./messages/messages.component";
import { LikesComponent } from "./likes/likes.component";
import { AuthGuard } from "./_guards/auth.guard";
import { MemberDetailComponent } from "./members/member-detail/member-detail.component";
import { MemberDetailResolver } from "./_resolvers/member-detail.resolver";
import { MembersResolver } from "./_resolvers/members.resolver";

export const appRoutes: Routes = [
  { path: "", component: HomeComponent },
  {
    path: "",
    runGuardsAndResolvers: "always",
    canActivate: [AuthGuard],
    children: [
      {
        path: "members",
        component: MembersComponent,
        resolve: { users: MembersResolver }
      },
      {
        path: "members/:id",
        component: MemberDetailComponent,
        resolve: { user: MemberDetailResolver }
      },
      { path: "messages", component: MessagesComponent },
      { path: "likes", component: LikesComponent }
    ]
  },
  { path: "**", redirectTo: "", pathMatch: "full" }
];
