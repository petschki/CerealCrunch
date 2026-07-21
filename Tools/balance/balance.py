"""Verifies the level formula: simulated win rate per level.

Player model: picks the better of 2 random valid moves by immediate match
size (between random and greedy, like a casual player).
"""
import random
import statistics
from sim import fill_board, valid_moves, resolve, find_matches


def level_config(level):
    if level <= 3:
        types = 5
        ppm = 140 + 60 * (level - 1)          # 140, 200, 260
    elif level <= 7:
        types = 6
        ppm = 85 + 20 * (level - 4)           # 85..145
    else:
        types = 7
        ppm = 60 + 12 * (level - 8)           # 60, 72, ...
    moves = min(11 + level, 22)
    target = round(moves * ppm / 10) * 10
    return types, moves, target


def play_level(types, moves, target):
    board = fill_board(types)
    score = 0
    for _ in range(moves):
        mv = valid_moves(board)
        if not mv:
            board = fill_board(types)
            mv = valid_moves(board)
            if not mv:
                continue
        a, b, _ = max(random.sample(mv, min(2, len(mv))), key=lambda m: m[2])
        board[a[0]][a[1]], board[b[0]][b[1]] = board[b[0]][b[1]], board[a[0]][a[1]]
        score += resolve(board, types)
        if score >= target:
            return True
    return score >= target


def main():
    random.seed(7)
    games = 50
    print(f"{'level':>5} {'types':>6} {'moves':>5} {'target':>7} {'win rate':>10}")
    for level in range(1, 13):
        types, moves, target = level_config(level)
        wins = sum(play_level(types, moves, target) for _ in range(games))
        print(f"{level:>5} {types:>6} {moves:>5} {target:>7} {wins / games:>9.0%}")


if __name__ == "__main__":
    main()
