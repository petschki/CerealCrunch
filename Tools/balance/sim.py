"""Monte-Carlo-Simulation der CerealCrunch-Match-Regeln zur Level-Balance.

Repliziert die Logik aus CerealBoard.cs:
- gerade Reihen ab 3
- 2x2-Quadrate
- Ueber-Eck-Erweiterung (2er-Reihen an gematchten Teilen, Fixpunkt)
- Kaskaden: score += count * 10 * cascade
"""
import random
import statistics

W = H = 8


def random_type_without_match(board, x, y, types):
    banned = set()
    if x >= 2 and board[x - 1][y] == board[x - 2][y]:
        banned.add(board[x - 1][y])
    if y >= 2 and board[x][y - 1] == board[x][y - 2]:
        banned.add(board[x][y - 1])
    if x >= 1 and y >= 1 and board[x - 1][y] == board[x - 1][y - 1] == board[x][y - 1]:
        banned.add(board[x - 1][y])
    while True:
        t = random.randrange(types)
        if t not in banned:
            return t


def fill_board(types):
    board = [[None] * H for _ in range(W)]
    for x in range(W):
        for y in range(H):
            board[x][y] = random_type_without_match(board, x, y, types)
    return board


def find_matches(board):
    matched = [[False] * H for _ in range(W)]

    # gerade Reihen
    for y in range(H):
        x = 0
        while x < W:
            run = 1
            while x + run < W and board[x + run][y] == board[x][y]:
                run += 1
            if run >= 3:
                for i in range(run):
                    matched[x + i][y] = True
            x += run
    for x in range(W):
        y = 0
        while y < H:
            run = 1
            while y + run < H and board[x][y + run] == board[x][y]:
                run += 1
            if run >= 3:
                for i in range(run):
                    matched[x][y + i] = True
            y += run

    # 2x2-Quadrate
    for x in range(W - 1):
        for y in range(H - 1):
            if board[x][y] == board[x + 1][y] == board[x][y + 1] == board[x + 1][y + 1]:
                matched[x][y] = matched[x + 1][y] = matched[x][y + 1] = matched[x + 1][y + 1] = True

    # Ueber-Eck-Erweiterung
    changed = True
    while changed:
        changed = False
        for y in range(H):
            x = 0
            while x < W:
                run = 1
                while x + run < W and board[x + run][y] == board[x][y]:
                    run += 1
                if run >= 2:
                    cells = [(x + i, y) for i in range(run)]
                    flags = [matched[cx][cy] for cx, cy in cells]
                    if any(flags) and not all(flags):
                        for cx, cy in cells:
                            matched[cx][cy] = True
                        changed = True
                x += run
        for x in range(W):
            y = 0
            while y < H:
                run = 1
                while y + run < H and board[x][y + run] == board[x][y]:
                    run += 1
                if run >= 2:
                    cells = [(x, y + i) for i in range(run)]
                    flags = [matched[cx][cy] for cx, cy in cells]
                    if any(flags) and not all(flags):
                        for cx, cy in cells:
                            matched[cx][cy] = True
                        changed = True
                y += run

    return [(x, y) for x in range(W) for y in range(H) if matched[x][y]]


def resolve(board, types):
    """Kaskaden aufloesen, Punkte wie im Spiel zaehlen."""
    score = 0
    cascade = 0
    while True:
        matches = find_matches(board)
        if not matches:
            break
        cascade += 1
        score += len(matches) * 10 * cascade
        for x, y in matches:
            board[x][y] = None
        for x in range(W):
            col = [board[x][y] for y in range(H) if board[x][y] is not None]
            col += [random.randrange(types) for _ in range(H - len(col))]
            for y in range(H):
                board[x][y] = col[y]
    return score


def valid_moves(board):
    moves = []
    for x in range(W):
        for y in range(H):
            for dx, dy in ((1, 0), (0, 1)):
                nx, ny = x + dx, y + dy
                if nx >= W or ny >= H:
                    continue
                board[x][y], board[nx][ny] = board[nx][ny], board[x][y]
                n = len(find_matches(board))
                board[x][y], board[nx][ny] = board[nx][ny], board[x][y]
                if n > 0:
                    moves.append(((x, y), (nx, ny), n))
    return moves


def play_game(types, num_moves, greedy):
    board = fill_board(types)
    total = 0
    for _ in range(num_moves):
        moves = valid_moves(board)
        if not moves:
            board = fill_board(types)
            moves = valid_moves(board)
            if not moves:
                continue
        if greedy:
            (a, b, _) = max(moves, key=lambda m: m[2])
        else:
            (a, b, _) = random.choice(moves)
        board[a[0]][a[1]], board[b[0]][b[1]] = board[b[0]][b[1]], board[a[0]][a[1]]
        total += resolve(board, types)
    return total


def main():
    random.seed(42)
    games = 80
    num_moves = 15
    print(f"{games} Partien x {num_moves} Zuege, 8x8")
    print(f"{'Sorten':>6} {'Strategie':>10} {'Punkte/Zug Ø':>14} {'Median':>8} {'P25':>8} {'P75':>8}")
    for types in (4, 5, 6):
        for greedy in (False, True):
            results = [play_game(types, num_moves, greedy) / num_moves for _ in range(games)]
            name = "greedy" if greedy else "random"
            q = statistics.quantiles(results, n=4)
            print(f"{types:>6} {name:>10} {statistics.mean(results):>14.1f} "
                  f"{statistics.median(results):>8.1f} {q[0]:>8.1f} {q[2]:>8.1f}")


if __name__ == "__main__":
    main()
