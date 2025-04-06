// SPDX-FileCopyrightText: 2024 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System.Net;

namespace MuninNode.AccessRules;

public interface IAccessRule {
  bool IsAcceptable(IPEndPoint remoteEndPoint);
}
